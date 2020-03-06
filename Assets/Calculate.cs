using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;

public class Calculate : MonoBehaviour
{
    const int max = 10_000;
    const int frameMax = max * max / 5;

    struct Result
    {
        public float value;
    }

    struct ToughJob : IJobParallelFor
    {
        public long offset;
        [NativeDisableParallelForRestriction] public NativeArray<Result> result;

        public void Execute(int index)
        {
            var i1 = (offset + index) / max;
            var i2 = (offset + index) % max;
            long product = i1 * i2;

            var output = result[0];
            if (output.value > product)
                output.value = product;
        }
    }

    IEnumerable<float> ToughTaskJobbed()
    {
        long processed = 0;

        int total = max * max;
        NativeArray<Result> nativeResult = new NativeArray<Result>(1, Allocator.TempJob);
        nativeResult[0] = new Result
        {
            value = float.MaxValue
        };

        while (processed < total)
        {
            new ToughJob
            {
                offset = 0,
                result = nativeResult,
            }.Schedule(frameMax, 1).Complete();

            processed += frameMax;
            yield return nativeResult[0].value;
        }

        var result = nativeResult[0].value;
        nativeResult.Dispose();
        yield return result;
    }


    IEnumerable<float> ToughTask()
    {
        long result = 0;
        long processed = 0;

        for (int i = 0; i < max; i++)
        {
            for (int j = 0; j < max; j++)
            {
                var dot = i * j;
                if (dot > result)
                    result = dot;

                processed++;
                if (processed > frameMax)
                {
                    processed = 0;
                    yield return result;
                }
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        var start = Time.realtimeSinceStartup;
        Debug.Log($"Started");

        //var process = ToughTask();
        var process = ToughTaskJobbed();
        foreach (var result in process)
        {
            Debug.Log($"FrameProcessing: {Time.realtimeSinceStartup - start}");
        }

        Debug.Log($"Finished: {Time.realtimeSinceStartup - start}");
    }
}
