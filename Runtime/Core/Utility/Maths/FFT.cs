using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;

using Rayforge.Utility.Maths;

using static Unity.Mathematics.math;
using System;

namespace Rayforge.Utility.Maths.FFT
{
    [BurstCompile]
    public struct FFTJob : IJob
    {
        public NativeArray<Complex> _Samples;

        public void Execute()
        {
            
        }

        private void SwapRange(int offset, int N)
        {
            for (int n = 1; n < (N >> 1); n += 2)
            {
                int index0 = offset + n;
                int index1 = offset + n + (N >> 1) - 1;

                var tmp = _Samples[index0];
                _Samples[index0] = _Samples[index1];
                _Samples[index1] = tmp;
            }
        }

        private void Swap(int offset, int N)
        {
            if (N == 2)
                return;

            int sub = N >> 1;
            SwapRange(offset, N);
            Swap(offset, sub);
            Swap(offset + sub, sub);
        }

        private void Revert(int offset, int N)
        {
            if (N == 2)
                return;

            int sub = N >> 1;
            Revert(offset, sub);
            Revert(offset + sub, sub);
            SwapRange(offset, N);
        }


        private void FFT(int offset, int N)
        {
            if (N == 1)
                return;

            Swap(offset, N);

            int sub = N >> 1;
            FFT(offset, sub);
            FFT(offset + sub, sub);

            for (int k = 0; k < (N >> 1); k++)
            {
                float ang = (float)(2 * PI * k / N);

                var p = _Samples[k];
                var _q = _Samples[k + (N >> 1)];

                var q = (new Polar(1.0f, (float)(-2.0f * PI * (float)k / N))).ToComplex() * _q;

                _Samples[k] = p + q;
                _Samples[k + (N >> 1)] = p - q;
            }
        }
    }

    public static class FFTJobDispatcher
    {

    }
}
