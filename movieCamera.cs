using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class movieCamera : MonoBehaviour
{
    public float viewRadius;
    [Range(0, 360)]
    public float viewAngle;

    public LayerMask filmableMask;
    public LayerMask obstacleMask;

    public Director director;

    public float meshResolution;

    public MeshFilter viewMeshFilter;
    Mesh viewMesh;

    public int binarySearchIters;

    private void Start()
    {
        viewMesh = new Mesh();
        viewMesh.name = "View Mesh";
        viewMeshFilter.mesh = viewMesh;
        StartCoroutine("FindTargetsWithDelay", .2f);
    }

    private void LateUpdate()
    {
        drawFieldOfView();
    }

    IEnumerator FindTargetsWithDelay(float delay)
    {
        while (true)
        {
            yield return new WaitForSeconds(delay);
            FindVisibleTargets();
        }
    } 

    void FindVisibleTargets()
    {
        Collider[] targetsInView = Physics.OverlapSphere(transform.position, viewRadius, filmableMask);

        for (int i = 0; i<targetsInView.Length; i++)
        {
            Transform target = targetsInView[i].transform;
            Vector3 dirToTarget = (target.position - transform.position).normalized;
            if (Vector3.Angle(transform.forward, dirToTarget) < viewAngle/2)
            {
                float dstToTarget = Vector3.Distance(transform.position, target.position);
                if (!Physics.Raycast(transform.position, dirToTarget, dstToTarget, obstacleMask))
                {
                    filmableEntity onCameraEntity = target.transform.parent.gameObject.GetComponent<filmableEntity>();
                    onCameraEntity.currentlyInShot = true;
                    onCameraEntity.lastTimeShot = Time.time;
                    director.filmableEntitiesInShot.Add(onCameraEntity);
                }
            }
        }
    }

    public void drawFieldOfView() {
        int stepCount = Mathf.RoundToInt(viewAngle * meshResolution);
        float stepAngleSize = viewAngle / stepCount;
        List<Vector3> viewPoints = new List<Vector3>();
        viewCastInfo oldViewCast = new viewCastInfo();
        for (int i = 0; i <= stepCount; i++)
        {
            float angle = transform.eulerAngles.y - viewAngle / 2 + stepAngleSize * i;
            viewCastInfo newViewCast = viewCast(angle);

            if (i > 0)
            {
                if (oldViewCast.hit != newViewCast.hit)
                {
                    edgeInfo edge = findEdge(oldViewCast, newViewCast);
                    if (edge.pointA != Vector3.zero)
                        viewPoints.Add(edge.pointA);
                    if (edge.pointB != Vector3.zero)
                        viewPoints.Add(edge.pointB);
                }
            }

            viewPoints.Add(newViewCast.point);
            oldViewCast = newViewCast;
        }

        int vertexCount = viewPoints.Count + 1;
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[(vertexCount - 2) * 3];

        vertices[0] = Vector3.zero;
        for (int i = 0; i < vertexCount - 1; i++)
        {
            vertices[i + 1] = transform.InverseTransformPoint(viewPoints[i]);

            if (i < vertexCount - 2)
            {
                triangles[(i) * 3] = 0;
                triangles[(i) * 3 + 1] = i;
                triangles[(i) * 3 + 2] = i + 1;
            }
        }

        viewMesh.Clear();
        viewMesh.vertices = vertices;
        viewMesh.triangles = triangles;
        viewMesh.RecalculateNormals();
    }

    edgeInfo findEdge(viewCastInfo minViewCast, viewCastInfo maxViewCast)
    {
        float minAngle = minViewCast.angle;
        float maxAngle = maxViewCast.angle;
        Vector3 minPoint = Vector3.zero;
        Vector3 maxPoint = Vector3.zero;
        for (int i = 0; i < binarySearchIters; i++)
        {
            float angle = (minAngle + maxAngle) / 2;
            viewCastInfo newViewCast = viewCast(angle);
            if (newViewCast.hit == minViewCast.hit)
            {
                minAngle = angle;
                minPoint = newViewCast.point;
            } else
            {
                maxAngle = angle;
                maxPoint = newViewCast.point;
            }
        }
        return new edgeInfo(minPoint, maxPoint);
    }

    viewCastInfo viewCast(float globalAngle)
    {
        Vector3 dir = dirFromAngle(globalAngle, true);
        RaycastHit hit;

        if (Physics.Raycast(transform.position, dir, out hit, viewRadius, obstacleMask))
        {
            return new viewCastInfo(true, hit.point, hit.distance, globalAngle);
        } else
        {
            return new viewCastInfo(false, transform.position + dir * viewRadius, viewRadius, globalAngle);
        }
    }

    public Vector3 dirFromAngle(float angleDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal)
        {
            angleDegrees += transform.eulerAngles.y;
        }
        return new Vector3(Mathf.Sin(angleDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleDegrees * Mathf.Deg2Rad));
    }

    public struct viewCastInfo
    {
        public bool hit;
        public Vector3 point;
        public float dst;
        public float angle;

        public viewCastInfo(bool _hit, Vector3 _point, float _dst, float _angle)
        {
            hit = _hit;
            point = _point;
            dst = _dst;
            angle = _angle;
        }
    }

    public struct edgeInfo
    {
        public Vector3 pointA;
        public Vector3 pointB;

        public edgeInfo(Vector3 _pointA, Vector3 _pointB)
        {
            pointA = _pointA;
            pointB = _pointB;
        }
    }
}
