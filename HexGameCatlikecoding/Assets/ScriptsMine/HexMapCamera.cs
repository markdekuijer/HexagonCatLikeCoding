using UnityEngine;

public class HexMapCamera : MonoBehaviour
{
    static HexMapCamera instance;

    Transform swivel, stick;
    float zoom = 1f;
    float rotationAngle;

    public float stickMinZoom, stickMaxZoom;
    public float swivelMinZoom, swivelMaxZoom;

    public float zoomedInMovementSpeed;
    public float zoomedOutMovementSpeed;
    public HexGrid grid;

    public float rotationSpeed;

    public static bool Locked
    {
        set
        {
            instance.enabled = !value;
        }
    }

    public static void ValidatePosition()
    {
        instance.AdjustPosition(0f, 0f);
    }

    void Awake()
    {
        instance = this;
        swivel = transform.GetChild(0);
        stick = swivel.GetChild(0);
    }

    void Update()
    {
        float zoomDelta = Input.GetAxis("Mouse ScrollWheel");
        if(zoomDelta != 0f)
            AdjustZoom(zoomDelta);

        float rotationDelta = Input.GetAxis("Rotation");
        if (rotationDelta != 0)
            AdjustRotation(rotationDelta);

        float deltaX = Input.GetAxis("Horizontal");
        float deltaZ = Input.GetAxis("Vertical");
        if (deltaX != 0 || deltaZ != 0)
            AdjustPosition(deltaX, deltaZ);
    }

    void AdjustZoom(float delta)
    {
        zoom = Mathf.Clamp01(zoom + delta);

        float distance = Mathf.Lerp(stickMinZoom, stickMaxZoom, zoom);
        stick.localPosition = new Vector3(0f, 0f, distance);

        float angle = Mathf.Lerp(swivelMinZoom, swivelMaxZoom, zoom);
        swivel.localRotation = Quaternion.Euler(angle, 0f, 0f);
    }

    public void AdjustRotation(float rotationDelta)
    {
        rotationAngle += rotationDelta * rotationSpeed * Time.deltaTime;

        if (rotationAngle < 0)
            rotationAngle += 360;
        else if (rotationAngle >= 360)
            rotationAngle -= 360;

        transform.localRotation = Quaternion.Euler(0, rotationAngle, 0);
    }

    public void AdjustPosition(float x, float z)
    {
        float movementSpeed = Mathf.Lerp(zoomedOutMovementSpeed, zoomedInMovementSpeed, zoom);
        Vector3 direction = transform.localRotation * new Vector3(x, 0, z).normalized;
        float damping = Mathf.Max(Mathf.Abs(x), Mathf.Abs(z));
        float distance = movementSpeed * damping * Time.deltaTime;

        Vector3 position = transform.localPosition;
        position += direction * distance;
        transform.localPosition = ClampPosition(position);
    }

    Vector3 ClampPosition(Vector3 position)
    {
        float maxX = (grid.cellCountX - 0.5f) * (HexMetrics.innerRadius * 2f);
        position.x = Mathf.Clamp(position.x, 0, maxX);

        float maxZ = (grid.cellCountZ - 1) * (HexMetrics.outerRadius * 1.5f);
        position.z = Mathf.Clamp(position.z, 0, maxZ);

        return position;
    }
}
