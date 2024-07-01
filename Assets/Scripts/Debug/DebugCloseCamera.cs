using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugCloseCamera : MonoBehaviour
{
    public TransformData origin;
    public Vector3 StartPoint;
    Vector3 AddedPos;

    public TransformData CloseTransform;

    public float ZoomRate;
    public float ZoomSpeed = 1f;
    public float MoveSpeed = 3f;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            origin = new TransformData(Camera.main.transform);
            AddedPos = Vector3.zero;

            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            var cameraTrans = Camera.main.transform;
            if (Physics.Raycast(cameraTrans.position, ray.direction, out var hitInfo))
                StartPoint = hitInfo.point;
        }

        if (Input.GetMouseButton(0))
        {
            ZoomRate = Mathf.Min(1 , ZoomRate + Time.deltaTime * ZoomSpeed);
        }
        else if (ZoomRate > 0)
        {
            ZoomRate = Mathf.Max(0, ZoomRate - Time.deltaTime * ZoomSpeed);
        }

        if (ZoomRate > 0)
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            var cameraTrans = Camera.main.transform;
            if (Physics.Raycast(cameraTrans.position, ray.direction, out var hitInfo))
            {
                AddedPos += MoveSpeed * Time.deltaTime * (hitInfo.point - (StartPoint + AddedPos)).normalized;
                Camera.main.transform.SetPositionAndRotation(
                    Vector3.Lerp(origin.position, StartPoint + CloseTransform.Position, ZoomRate) + AddedPos,
                    Quaternion.Lerp(origin.rotation, CloseTransform.rotation, ZoomRate));
            }
        }

    }
}
