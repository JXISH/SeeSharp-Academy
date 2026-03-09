using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace Try_MathNET_Using_MKL
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // 1. 先启用MKL加速（已验证成功）
            //var mklAvailable = Control.TryUseNativeMKL();
            //Console.WriteLine($"MKL 是否可用：{mklAvailable}");
            Control.UseManaged();

            // 矩阵尺寸
            int rows = 1066;
            int columns = 1066;

            // ========== 方式1：生成【均匀分布】随机矩阵（0~1之间）==========
            // 创建均匀分布对象（最小值，最大值，随机数种子）
            var uniformDistribution = new ContinuousUniform(0, 1, new Random(42));
            DenseMatrix matrix1 = DenseMatrix.CreateRandom(rows, columns, uniformDistribution);
            Console.WriteLine($"均匀分布矩阵示例值：{matrix1[0, 0]:F4}");

            // ========== 方式2：生成【正态分布】随机矩阵（均值0，标准差1）==========
            // 正态分布（均值，标准差，随机数种子）
            var normalDistribution = new Normal(0, 1, new Random(42));
            DenseMatrix matrix2 = DenseMatrix.CreateRandom(rows, columns, normalDistribution);
            Console.WriteLine($"正态分布矩阵示例值：{matrix2[0, 0]:F4}");

            // ========== 测试MKL加速的SVD ==========
            var sw = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < 10; i++)
            {
                var svd = matrix1.Svd(true);
            }
            sw.Stop();
            Console.WriteLine($"{rows}x{columns}矩阵SVD耗时（MKL加速）：{sw.ElapsedMilliseconds / 10.0} ms");
        }
    }
}
