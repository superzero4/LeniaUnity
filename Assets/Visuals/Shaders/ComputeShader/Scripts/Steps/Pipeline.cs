using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

public class Pipeline : MonoBehaviour, IComputeBufferProvider
{
    [SerializeField, Range(0, 10f)] private float _initDelay;
    [SerializeField, Range(0, 10f)] private float _delay;
    [SerializeField] private bool _runOnce = false;

    private IInitValues _info;
    private ComputeBuffer _buffer1;
    private ComputeBuffer _buffer2;

    private bool _toggle;
    public ComputeBuffer ReadBuffer => _toggle ? _buffer1 : _buffer2;
    public ComputeBuffer Buffer => _toggle ? _buffer2 : _buffer1;
    public Vector3Int Size3D => _info.Size;
    private IStep[] _steps;

    private IEnumerator Start()
    {
        var init = GetComponentInChildren<IInitValues>(false);
        IStep[] steps = GetComponentsInChildren<IStep>(false);
        ReleaseBuffers();
        Init(init);
        Assert.IsNotNull(init, "Init values not found");
        Debug.Log("Init values : " +
                  init.GetType().Name + "and steps : " + string.Join(", ", steps.Select(st => st.GetType().Name)));
        foreach (IStep step in steps)
            step.Init(init);

        yield return new WaitForSeconds(_initDelay);
        do
        {
            foreach (IStep step in steps)
                yield return step.Step(_delay);
        } while (!_runOnce);
    }

    private void Init(IInitValues init)
    {
        Assert.IsNotNull(init, "InitValues is null");
        _info = init;
        var size = _info.TotalSize;
        Debug.Log($"Total size of the space : {size}");
        _buffer1 = new ComputeBuffer(size, sizeof(float));
        _buffer2 = new ComputeBuffer(size, sizeof(float));


        var values = init.InitialValues();
        _buffer1.SetData(values);
        _buffer2.SetData(values);
    }

    private void ReleaseBuffers()
    {
        _buffer1?.Release();
        _buffer2?.Release();
        foreach (IStep step in _steps)
            step.Release();
    }

    private void OnDisable()
    {
        ReleaseBuffers();
    }

    private void OnDestroy()
    {
        ReleaseBuffers();
    }

    private void OnApplicationQuit()
    {
        ReleaseBuffers();
    }
}