const express = require('express');
const redis = require('redis');
const cors = require('cors');
const path = require('path');

const app = express();
const PORT = 3000;

// 启用CORS
app.use(cors());
app.use(express.json());
app.use(express.static(__dirname));

// 创建Redis客户端（Redis v4+ API）
const redisClient = redis.createClient({
    socket: {
        host: 'localhost',
        port: 6379
    }
});

// 连接到Redis
redisClient.connect().catch(err => {
    console.error('Redis连接失败:', err);
});

redisClient.on('error', (err) => {
    console.error('Redis连接错误:', err);
});

redisClient.on('connect', () => {
    console.log('已连接到Redis服务器');
});

redisClient.on('ready', () => {
    console.log('Redis客户端准备就绪');
});

// API端点：获取电机数据
app.get('/api/motor-data', async (req, res) => {
    try {
        // 从Redis获取所有相关数据（使用Promise API）
        const [timeWaveform, waveformDT, spectrum, spectrumDF, frequencies, amplitudes] = await Promise.all([
            redisClient.get('SeesharpMotor:TimeWaveform'),
            redisClient.get('SeesharpMotor:Waveform_dT'),
            redisClient.get('SeesharpMotor:Spectrum'),
            redisClient.get('SeesharpMotor:Spectrum_dF'),
            redisClient.get('SeesharpMotor:Frequencies'),
            redisClient.get('SeesharpMotor:Amplitudes')
        ]);

        // 解析数据
        const data = {
            TimeWaveform: timeWaveform ? timeWaveform.split(',').map(Number) : [],
            Waveform_dT: waveformDT ? parseFloat(waveformDT) : 0.001,
            Spectrum: spectrum ? spectrum.split(',').map(Number) : [],
            Spectrum_dF: spectrumDF ? parseFloat(spectrumDF) : 1.0,
            Frequencies: frequencies ? frequencies.split(',').map(Number) : [],
            Amplitudes: amplitudes ? amplitudes.split(',').map(Number) : []
        };

        res.json(data);
    } catch (error) {
        console.error('获取Redis数据错误:', error);
        res.status(500).json({ error: '获取数据失败' });
    }
});

// 启动服务器
app.listen(PORT, () => {
    console.log(`服务器运行在 http://localhost:${PORT}`);
    console.log(`访问 http://localhost:${PORT}/motor_monitor.html 查看监测界面`);
});

// 优雅关闭
process.on('SIGINT', async () => {
    console.log('\n正在关闭服务器...');
    await redisClient.quit();
    process.exit(0);
});
