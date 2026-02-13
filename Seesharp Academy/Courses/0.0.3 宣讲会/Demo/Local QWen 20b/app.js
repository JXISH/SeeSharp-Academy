/* ---------- 全局变量 ---------- */
const acquireBtn = document.getElementById('acquireBtn');
const analyzeBtn = document.getElementById('analyzeBtn');

const chInput     = document.getElementById('channels');
const srInput     = document.getElementById('sampleRate');
const rangeInput  = document.getElementById('range');
const lenInput    = document.getElementById('length');

const waveCanvas1 = document.getElementById('wave1');
const histCanvas  = document.getElementById('histogram');

const ctxWave1 = waveCanvas1.getContext('2d');
const ctxHist  = histCanvas.getContext('2d');

let channelData = [];   // 每个通道的数据数组
let acquired     = false; // 是否已采集

/* ---------- 工具函数 ---------- */

// 把数组绘制成波形（线条）
function drawWave(ctx, data, color='#0074D9') {
    const w = ctx.canvas.width, h = ctx.canvas.height;
    ctx.clearRect(0, 0, w, h);
    ctx.strokeStyle = color;
    ctx.lineWidth = 1.5;
    ctx.beginPath();
    for (let i = 0; i < data.length; i++) {
        const x = (i / data.length) * w;
        const y = h/2 - (data[i] / 10) * (h/2); // 归一化到 [-1,1]
        if (i === 0) ctx.moveTo(x, y);
        else ctx.lineTo(x, y);
    }
    ctx.stroke();
}

// 生成正弦波
function genSine(freq, amplitude, len, sampleRate) {
    const arr = new Float32Array(len);
    for (let i = 0; i < len; i++) {
        const t = i / sampleRate;
        arr[i] = amplitude * Math.sin(2 * Math.PI * freq * t);
    }
    return arr;
}

// 绘制直方图
function drawHistogram(ctx, data, bins=20) {
    const w = ctx.canvas.width, h = ctx.canvas.height;
    ctx.clearRect(0, 0, w, h);

    // 找到最大最小值
    let min = Infinity, max = -Infinity;
    data.forEach(v => { if (v < min) min = v; if (v > max) max = v; });

    const binSize = (max - min) / bins;
    const hist = new Array(bins).fill(0);
    data.forEach(v => {
        let idx = Math.floor((v - min) / binSize);
        if (idx === bins) idx--; // 处理极值
        hist[idx]++;
    });

    // 找到最高柱子
    const maxCount = Math.max(...hist);

    ctx.fillStyle = '#FF4136';
    for (let i = 0; i < bins; i++) {
        const barHeight = (hist[i] / maxCount) * h;
        const x = (i / bins) * w;
        const y = h - barHeight;
        const bw = w / bins * 0.8;
        ctx.fillRect(x, y, bw, barHeight);
    }
}

/* ---------- 事件处理 ---------- */

// 采集
acquireBtn.onclick = () => {
    const chStr   = chInput.value.trim();
    const sr      = parseInt(srInput.value) || 10000;
    const range   = parseFloat(rangeInput.value) || 10;
    const len     = parseInt(lenInput.value) || 2000;

    // 解析通道
    const chList = chStr.split(',').map(s => s.trim()).filter(Boolean);
    if (chList.length === 0) {
        alert('请至少输入一个通道编号');
        return;
    }

    channelData = [];
    chList.forEach((ch, idx) => {
        // 为演示起见，给每个通道不同频率
        const freq = 5 + idx * 3;            // 5Hz,8Hz,...
        const amp  = range / 2;              // 量程的一半
        channelData.push(genSine(freq, amp, len, sr));
    });

    // 把第一个通道绘制到波形一（如果有多个通道，可根据需求改为多条线）
    drawWave(ctxWave1, channelData[0], '#0074D9');

    acquired = true;
};

// 分析
analyzeBtn.onclick = () => {
    if (!acquired) { alert('请先采集数据！'); return; }

    // 这里我们以第一个通道的数据做直方图演示
    drawHistogram(ctxHist, channelData[0], 30);
};

