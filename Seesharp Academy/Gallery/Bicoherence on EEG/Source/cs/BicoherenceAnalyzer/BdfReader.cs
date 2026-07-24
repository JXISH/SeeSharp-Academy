using System.Globalization;

namespace BicoherenceAnalyzer;

/// <summary>
/// BDF (Biosemi Data Format) 文件读取器 - 支持Biosemi字段打包格式
/// 
/// 该数据集使用Biosemi特殊的BDF变体，通道头以"按字段类型打包"的方式存储：
///   所有通道的Label依次存放，然后是所有通道的Transducer，依此类推。
///   而不是标准EDF/BDF的"每个通道独立256字节块"格式。
///   
/// 24位采样数据以3字节小端序有符号整数存储，需转换为μV物理值。
/// </summary>
public class BdfReader : IDisposable
{
    // === BDF 固定文件头字段 ===
    public string Version { get; private set; } = "";
    public string PatientId { get; private set; } = "";
    public string RecordingId { get; private set; } = "";
    public int NumDataRecords { get; private set; }
    public double RecordDuration { get; private set; }
    public int NumSignals { get; private set; }

    // === 通道信息 ===
    public BdfChannel[] Channels { get; private set; } = Array.Empty<BdfChannel>();

    // === 内部状态 ===
    private readonly FileStream _fileStream;
    private readonly BinaryReader _reader;
    private readonly long _dataStartOffset;     // 数据区起始偏移量
    private readonly int _totalSamplesPerRecord; // 每条记录的总采样点数
    private readonly int _bytesPerRecord;        // 每条记录的字节数

    // === Biosemi BDF 默认校准值 (24位) ===
    private const double DefaultDigitalMin = -8388608.0;
    private const double DefaultDigitalMax = 8388607.0;
    // Biosemi ActiveTwo: LSB ≈ 31.25 nV, 24-bit full scale ≈ ±262 mV
    // 物理范围 ≈ 262144 μV (已根据BDF数据验证)
    private const double DefaultPhysicalMin = -262144.0;
    private const double DefaultPhysicalMax = 262144.0;

    /// <summary>
    /// 构造函数：打开BDF文件并解析文件头
    /// </summary>
    public BdfReader(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"BDF文件未找到: {filePath}");

        _fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        _reader = new BinaryReader(_fileStream);

        // 确保文件至少有固定头
        if (_fileStream.Length < 256)
            throw new InvalidDataException("BDF文件小于256字节，无效格式");

        // 步骤1: 解析256字节固定文件头
        ParseFixedHeader();

        // 步骤2: 读取整个文件头到内存
        int totalHeaderBytes = 256 + NumSignals * 256; // 固定头 + 通道头
        byte[] fullHeader = new byte[totalHeaderBytes];
        _fileStream.Seek(0, SeekOrigin.Begin);
        _fileStream.ReadExactly(fullHeader, 0, totalHeaderBytes);

        // 步骤3: 解析通道头（Biosemi字段打包格式）
        Channels = ParsePackedChannelHeaders(fullHeader, 256);

        // 步骤4: 设置数据区参数
        _dataStartOffset = totalHeaderBytes;
        _totalSamplesPerRecord = Channels.Sum(c => c.SamplesPerRecord);
        _bytesPerRecord = _totalSamplesPerRecord * 3; // 24位 = 3字节/样本

        // 验证文件大小
        if (NumDataRecords > 0)
        {
            long expectedSize = _dataStartOffset + (long)NumDataRecords * _bytesPerRecord;
            if (_fileStream.Length < expectedSize)
                Console.WriteLine($"[警告] 文件可能截断: 实际 {_fileStream.Length} < 预期 {expectedSize}");
        }

        Console.WriteLine($"[BDF] 版本: {Version.Trim()}, 通道: {NumSignals}, 记录: {NumDataRecords}, " +
            $"记录时长: {RecordDuration}s, 数据起始: {_dataStartOffset}");
    }

    /// <summary>
    /// 解析BDF固定文件头（前256字节ASCII文本）
    /// </summary>
    private void ParseFixedHeader()
    {
        byte[] headerBytes = _reader.ReadBytes(256);
        if (headerBytes.Length < 256)
            throw new InvalidDataException("BDF文件头不足256字节");

        Version = ReadAscii(headerBytes, 0, 8).Trim();
        PatientId = ReadAscii(headerBytes, 8, 80).Trim();
        RecordingId = ReadAscii(headerBytes, 88, 80).Trim();

        string numRecordsStr = ReadAscii(headerBytes, 236, 8).Trim();
        NumDataRecords = int.Parse(numRecordsStr, CultureInfo.InvariantCulture);

        string recordDurStr = ReadAscii(headerBytes, 244, 8).Trim();
        RecordDuration = ParseDoubleSafe(recordDurStr);

        string numSignalsStr = ReadAscii(headerBytes, 252, 4).Trim();
        NumSignals = int.Parse(numSignalsStr, CultureInfo.InvariantCulture);

        // 验证文件头字节数
        string headerBytesStr = ReadAscii(headerBytes, 184, 8).Trim();
        int declaredHeaderBytes = int.Parse(headerBytesStr, CultureInfo.InvariantCulture);
        int expected = 256 + NumSignals * 256;
        if (declaredHeaderBytes != expected)
            Console.WriteLine($"[警告] 声明头字节({declaredHeaderBytes}) ≠ 预期({expected})");
    }

    /// <summary>
    /// 解析Biosemi字段打包格式的通道头
    /// 
    /// 字段顺序（每种字段所有80个通道的值连续存放）:
    ///   Label(16B×N) → Transducer(80B×N) → PhysDim(8B×N) → PhysMin(8B×N) 
    ///   → PhysMax(8B×N) → DigMin(8B×N) → DigMax(8B×N) → Prefilter(80B×N)
    ///   → SamplesPerRec(8B×N) → Reserved(32B×N)
    /// </summary>
    private static BdfChannel[] ParsePackedChannelHeaders(byte[] fullHeader, int headerStart)
    {
        int N = (fullHeader.Length - headerStart) / 256;
        var channels = new BdfChannel[N];

        // 各字段在 packed header 中的起始偏移
        int offset = headerStart;
        int labelOffset = offset;                           offset += 16 * N;
        int transducerOffset = offset;                      offset += 80 * N;
        int physDimOffset = offset;                         offset += 8 * N;
        int physMinOffset = offset;                         offset += 8 * N;
        int physMaxOffset = offset;                         offset += 8 * N;
        int digMinOffset = offset;                          offset += 8 * N;
        int digMaxOffset = offset;                          offset += 8 * N;
        int prefilterOffset = offset;                       offset += 80 * N;
        int samplesOffset = offset;                         offset += 8 * N;
        // reservedOffset = offset;  (不需要解析保留字段)

        for (int i = 0; i < N; i++)
        {
            string label = ReadAscii(fullHeader, labelOffset + i * 16, 16).Trim();
            string physDim = ReadAscii(fullHeader, physDimOffset + i * 8, 8).Trim();
            string physMinStr = ReadAscii(fullHeader, physMinOffset + i * 8, 8).Trim();
            string physMaxStr = ReadAscii(fullHeader, physMaxOffset + i * 8, 8).Trim();
            string digMinStr = ReadAscii(fullHeader, digMinOffset + i * 8, 8).Trim();
            string digMaxStr = ReadAscii(fullHeader, digMaxOffset + i * 8, 8).Trim();
            string samplesStr = ReadAscii(fullHeader, samplesOffset + i * 8, 8).Trim();

            // 解析校准值（使用容错解析，非数字时使用默认值）
            double physMin = ParseDoubleSafe(physMinStr, DefaultPhysicalMin);
            double physMax = ParseDoubleSafe(physMaxStr, DefaultPhysicalMax);
            double digMin = ParseDoubleSafe(digMinStr, DefaultDigitalMin);
            double digMax = ParseDoubleSafe(digMaxStr, DefaultDigitalMax);

            // 如果校准值异常（如physMin==physMax），使用默认值
            if (Math.Abs(physMax - physMin) < 1e-6 || Math.Abs(digMax - digMin) < 1e-6)
            {
                physMin = DefaultPhysicalMin;
                physMax = DefaultPhysicalMax;
                digMin = DefaultDigitalMin;
                digMax = DefaultDigitalMax;
            }

            int samplesPerRec = 1;
            if (!string.IsNullOrEmpty(samplesStr))
                int.TryParse(samplesStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out samplesPerRec);

            channels[i] = new BdfChannel
            {
                Index = i,
                Label = label,
                PhysicalDimension = physDim,
                PhysicalMin = physMin,
                PhysicalMax = physMax,
                DigitalMin = digMin,
                DigitalMax = digMax,
                SamplesPerRecord = samplesPerRec
            };
        }

        return channels;
    }

    /// <summary>
    /// 获取通道名称列表
    /// </summary>
    public string[] GetChannelNames() => Channels.Select(c => c.Label).ToArray();

    /// <summary>
    /// 读取指定通道的全部EEG数据（转换为物理值 μV）
    /// </summary>
    public double[] ReadChannelData(int channelIndex)
    {
        return ReadChannelData(channelIndex, 0,
            NumDataRecords * Channels[channelIndex].SamplesPerRecord);
    }

    /// <summary>
    /// 读取指定通道在指定采样点范围内的EEG数据（μV）
    /// </summary>
    public double[] ReadChannelData(int channelIndex, int startSample, int sampleCount)
    {
        var channel = Channels[channelIndex];
        int sps = channel.SamplesPerRecord;  // 该通道每记录的采样点数
        int totalSamples = NumDataRecords * sps;

        // 边界检查
        if (startSample < 0) startSample = 0;
        if (startSample + sampleCount > totalSamples)
            sampleCount = totalSamples - startSample;
        if (sampleCount <= 0)
            return Array.Empty<double>();

        double[] data = new double[sampleCount];

        // 计算该通道之前所有通道的样本偏移
        int precedingSamples = 0;
        for (int i = 0; i < channelIndex; i++)
            precedingSamples += Channels[i].SamplesPerRecord;

        // 校准参数
        double digRange = channel.DigitalMax - channel.DigitalMin;
        double physRange = channel.PhysicalMax - channel.PhysicalMin;
        double digMin = channel.DigitalMin;
        double physMin = channel.PhysicalMin;

        // 确定需要读取的记录范围
        int firstRecord = startSample / sps;
        int lastRecord = (startSample + sampleCount - 1) / sps;
        if (lastRecord >= NumDataRecords)
            lastRecord = NumDataRecords - 1;

        // 缓冲区：每条记录的所有采样（3字节/样本）
        int recordBytes = _bytesPerRecord;
        byte[] recordBuf = new byte[recordBytes];

        int dataIdx = 0;
        int offsetInFirst = startSample - firstRecord * sps;

        for (int rec = firstRecord; rec <= lastRecord; rec++)
        {
            long recordOff = _dataStartOffset + (long)rec * recordBytes;
            _fileStream.Seek(recordOff, SeekOrigin.Begin);
            int bytesRead = _fileStream.Read(recordBuf, 0, recordBytes);
            if (bytesRead < recordBytes)
                break;

            // 该通道在本记录中的起始字节位置
            int chByteStart = precedingSamples * 3;

            int sStart = (rec == firstRecord) ? offsetInFirst : 0;
            int sEnd = (rec == lastRecord) ? sps : sps;
            sEnd = Math.Min(sEnd, sps);

            for (int s = sStart; s < sEnd && dataIdx < sampleCount; s++)
            {
                int bytePos = chByteStart + s * 3;
                int raw = ReadInt24LE(recordBuf, bytePos);
                data[dataIdx++] = (raw - digMin) / digRange * physRange + physMin;
            }
        }

        if (dataIdx < sampleCount)
            Array.Resize(ref data, dataIdx);

        return data;
    }

    /// <summary>
    /// 读取指定时间范围（秒）内的EEG数据
    /// </summary>
    public double[] ReadChannelDataByTime(int channelIndex, double startTimeSec,
        double durationSec, int samplingRate)
    {
        int startSample = (int)(startTimeSec * samplingRate);
        int sampleCount = (int)(durationSec * samplingRate);
        return ReadChannelData(channelIndex, startSample, sampleCount);
    }

    /// <summary>
    /// 读取24位有符号整数（小端序，符号扩展）
    /// </summary>
    private static int ReadInt24LE(byte[] buf, int offset)
    {
        int value = buf[offset] | (buf[offset + 1] << 8) | (buf[offset + 2] << 16);
        if ((value & 0x800000) != 0)
            value |= unchecked((int)0xFF000000);
        return value;
    }

    /// <summary>从字节数组读取ASCII字符串</summary>
    private static string ReadAscii(byte[] buf, int offset, int len)
    {
        int actual = Math.Min(len, buf.Length - offset);
        return System.Text.Encoding.ASCII.GetString(buf, offset, actual);
    }

    /// <summary>安全解析double，失败返回默认值</summary>
    private static double ParseDoubleSafe(string s, double defaultValue = 0)
    {
        if (string.IsNullOrWhiteSpace(s)) return defaultValue;
        if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
            return result;
        return defaultValue;
    }

    public void Dispose()
    {
        _reader?.Dispose();
        _fileStream?.Dispose();
    }
}

/// <summary>
/// BDF通道信息
/// </summary>
public class BdfChannel
{
    public int Index { get; set; }
    public string Label { get; set; } = "";
    public string PhysicalDimension { get; set; } = "";
    public double PhysicalMin { get; set; }
    public double PhysicalMax { get; set; }
    public double DigitalMin { get; set; }
    public double DigitalMax { get; set; }
    public int SamplesPerRecord { get; set; }
}
