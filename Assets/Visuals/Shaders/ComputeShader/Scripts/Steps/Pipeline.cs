using System.Collections;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using Visuals.Shaders.ComputeShader.Scripts;

public class Pipeline : MonoBehaviour, IComputeBufferProvider
{
    [SerializeField, Range(0, 10f)] private float _initDelay;
    [SerializeField, Range(0, 10f)] private float _delay;
    [SerializeField] private bool _finalUpdateOnly = true;
    [SerializeField] private bool _runOnce = false;

    private IInitValues _info;
    private ComputeBuffer _shared;
    private ComputeBuffer _displayBuffer;

    public Vector3Int Size3D => _info.Size;
    private UnityEvent<ComputeBuffer> _onUpdate = new UnityEvent<ComputeBuffer>();
    public UnityEvent<ComputeBuffer> OnUpdate => _onUpdate;
    private IStep[] _steps;

    private IEnumerator Start()
    {
        var init = GetComponentInChildren<IInitValues>(false);
        _steps = GetComponentsInChildren<IStep>(false);
        ReleaseBuffers();
        Init(init);
        Assert.IsNotNull(init, "Init values not found");
        Debug.Log("Init values : " +
                  init.GetType().Name + "and steps : " + string.Join("\n",
                      _steps.Select(st => st.GetType().Name + " on " + (st as Component).gameObject.name)));
        foreach (IStep step in _steps)
            step.Init(init);

        yield return new WaitForSeconds(_initDelay);
        RaiseUpdate();
        while (!_runOnce)
        {
            yield return StepEnumerator();
        }
    }

    private IEnumerator StepEnumerator()
    {
        foreach (IStep step in _steps)
        {
            yield return step.Step(_shared, _delay);
            yield return new WaitForSeconds(_delay);
            if (!_finalUpdateOnly)
                RaiseUpdate();
        }

        yield return new WaitForSeconds(_delay);
        if (_finalUpdateOnly)
            RaiseUpdate();
    }

    private void RaiseUpdate()
    {
        StartCoroutine(RaiseUpdateRoutine());
    }

    private IEnumerator RaiseUpdateRoutine()
    {
        ShaderCommons.Copy(_shared, _displayBuffer);
        _onUpdate.Invoke(_displayBuffer);
        ShaderCommons.LogBuffer(_displayBuffer, "Display buffer after step");
        yield break;
    }

    [Button]
    public void StepOnce()
    {
        StartCoroutine(StepEnumerator());
    }

    private void Init(IInitValues init)
    {
        Assert.IsNotNull(init, "InitValues is null");
        _info = init;
        var size = _info.TotalSize;
        Debug.Log($"Total size of the space : {size}");
        _shared = new ComputeBuffer(size, sizeof(float));
        _displayBuffer = new ComputeBuffer(size, sizeof(float));
        var values = init.InitialValues();
        _shared.SetData(values);
    }

    private void ReleaseBuffers()
    {
        _shared?.Release();
        _displayBuffer?.Release();
        if (_steps != null)
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