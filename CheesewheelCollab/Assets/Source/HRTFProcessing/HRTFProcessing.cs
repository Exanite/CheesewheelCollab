using System.Collections;
using System.Collections.Generic;

public static class HRTFProcessing
{
    public static double[] Convolve(this double[] a, double[] kernel, bool trim)
    {
        double[] result;
        int m = (int)System.Math.Ceiling(kernel.Length / 2.0);

        if (trim)
        {
            result = new double[a.Length];

            for (int i = 0; i < result.Length; i++)
            {
                result[i] = 0;
                for (int j = 0; j < kernel.Length; j++)
                {
                    int k = i - j + m - 1;
                    if (k >= 0 && k < a.Length)
                        result[i] += a[k] * kernel[j];
                }
            }
        }
        else
        {
            result = new double[a.Length + m];

            for (int i = 0; i < result.Length; i++)
            {
                result[i] = 0;
                for (int j = 0; j < kernel.Length; j++)
                {
                    int k = i - j;
                    if (k >= 0 && k < a.Length)
                        result[i] += a[k] * kernel[j];
                }
            }
        }

        return result;
    }
}
