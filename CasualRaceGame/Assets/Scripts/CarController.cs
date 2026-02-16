using UnityEngine;

public class CarController : CarBase
{
    private float _inputVertical;
    private float _inputHorizontal;

    void Update()
    {
        // WS: 前進/後退
        _inputVertical = 0f;
        if (Input.GetKey(KeyCode.W)) _inputVertical = 1f;
        else if (Input.GetKey(KeyCode.S)) _inputVertical = -1f;

        // AD: 旋回
        _inputHorizontal = 0f;
        if (Input.GetKey(KeyCode.A)) _inputHorizontal = -1f;
        else if (Input.GetKey(KeyCode.D)) _inputHorizontal = 1f;
    }

    protected override void FixedUpdate()
    {
        if (_inputVertical > 0)
            Accelerate();
        else if (_inputVertical < 0)
            Brake();
        else
            Decelerate();

        _strafeInput = 0;

        Turn(_inputHorizontal);

        base.FixedUpdate();
    }
}