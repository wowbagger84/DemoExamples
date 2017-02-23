using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoV : MonoBehaviour {

    public float viewRadius;        //Range of view
    [Range(0,360)]
    public float viewAngle;         //ViewCone angle

    public LayerMask targetMask;    //Check if target layer
    public LayerMask obstacleMask;      //Check if objects is on obsticle Layer

    [HideInInspector]
    public List<Transform> visibleTargets = new List<Transform>();  //List of visible targets

    public float meshResolution;    //How many parts visualisation consists of.
    public int edgeDetectResolution;//Number of itteration to find the edges.
    public float edgeDetectTreshold;

    public float border = 0.1f;

    public MeshFilter viewMeshFilter;
    Mesh viewMesh;
    public MeshFilter fadeMeshFilter;
    Mesh fadeMesh;


	// Use this for initialization
	void Start () {

        //Setup for viewmesh
        viewMesh = new Mesh();
        viewMesh.name = "Veiw Mesh";
        viewMeshFilter.mesh = viewMesh;

        fadeMesh = new Mesh();
        fadeMesh.name = "Fade Mesh";
        fadeMeshFilter.mesh = fadeMesh;

        //Start new thread so we dont have to calc to often.
        StartCoroutine("FindTargetsWithDelay", 0.2f);
	}


    private void LateUpdate()
    {
        DrawFoV();
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
        visibleTargets.Clear(); //Clear the old list

        //Find all targets around the player
        Collider[] targetsInView = Physics.OverlapSphere(transform.position, viewRadius, targetMask);

        for (int i = 0; i < targetsInView.Length; i++)
        {
            Transform target = targetsInView[i].transform;

            //Get direction to target
            Vector3 dirToTarget = (target.position - transform.position).normalized;

            if (Vector3.Angle(transform.forward, dirToTarget) < viewAngle / 2)
            {
                //See if we have line of sight.
                if (!Physics.Raycast(transform.position, dirToTarget, viewRadius, obstacleMask))
                {
                    //Looking at target, add it to list.
                    visibleTargets.Add(target);
                }
            }
        }
    }
    

    void DrawFoV()
    {
        int stepCount = Mathf.RoundToInt(viewAngle * meshResolution);
        float stepAngleSize = viewAngle / stepCount;

        List<Vector3> viewPoints = new List<Vector3>();
        ViewCastInfo oldViewCast = new ViewCastInfo();

        for (int i = 0; i <= stepCount; i++)
        {
            float angle = transform.eulerAngles.y - viewAngle / 2 + stepAngleSize * i;
            //Debug.DrawLine(transform.position, transform.position + DirFromAngle(angle, true) * viewRadius, Color.red);
            ViewCastInfo newViewCast = ViewCast(angle);

            if (i > 0)
            {
                bool edgeDistExceeded = Mathf.Abs(oldViewCast.dist - newViewCast.dist) > edgeDetectTreshold;
                if (oldViewCast.hit != newViewCast.hit || (oldViewCast.hit && newViewCast.hit && edgeDistExceeded))
                {
                    EdgeInfo edge = FindEdge(oldViewCast, newViewCast);
                    if (edge.pointA != Vector3.zero)
                    {
                        viewPoints.Add(edge.pointA);
                    }
                    if (edge.pointB != Vector3.zero)
                    {
                        viewPoints.Add(edge.pointB);
                    }
                }
            }

            viewPoints.Add(newViewCast.point);
            oldViewCast = newViewCast;
        }

        int vertexCount = viewPoints.Count + 1;
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[(vertexCount - 2) * 3];

        vertices[0] = Vector3.zero;

        for (int i = 0; i < vertexCount-1; i++)
        {
            vertices[i + 1] = transform.InverseTransformPoint(viewPoints[i] + transform.forward * border);

            if (i < vertexCount - 2)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
        }


        //Create UV list
        Vector2[] uvs = new Vector2[vertexCount];

        for (int i = 0; i < vertices.Length; i++)
        {
            //Map UV depending on distance from center.
            float uv = 1 - (vertices[i].magnitude / viewRadius);
            uvs[i] = new Vector2(uv, uv);
        }

        //Clear and redraw mesh each frame
        viewMesh.Clear();
        viewMesh.vertices = vertices;
        viewMesh.triangles = triangles;
        viewMesh.uv = uvs;
        viewMesh.RecalculateNormals();

        fadeMesh.Clear();
        fadeMesh.vertices = vertices;
        fadeMesh.triangles = triangles;
        fadeMesh.uv = uvs;
        fadeMesh.RecalculateNormals();
    }


    ViewCastInfo ViewCast(float globalAngle)
    {
        Vector3 dir = DirFromAngle(globalAngle, true);
        RaycastHit hit;

        if (Physics.Raycast(transform.position, dir, out hit, viewRadius, obstacleMask))
        {
            return new ViewCastInfo(true, hit.point, hit.distance, globalAngle);
        }
        else
        {
            return new ViewCastInfo(false, transform.position + dir * viewRadius, viewRadius, globalAngle);
        }
    }


    EdgeInfo FindEdge(ViewCastInfo minViewCast, ViewCastInfo maxViewCast)
    {
        float minAngle = minViewCast.angle;
        float maxAngle = maxViewCast.angle;
        Vector3 minPoint = Vector3.zero;
        Vector3 maxPoint = Vector3.zero;

        for (int i = 0; i < edgeDetectResolution; i++)
        {
            float angle = (minAngle + maxAngle) / 2;
            ViewCastInfo newViewCast = ViewCast(angle);

            bool edgeDistExceeded = Mathf.Abs(minViewCast.dist - newViewCast.dist) > edgeDetectTreshold;
            if (newViewCast.hit == minViewCast.hit && !edgeDistExceeded)
            {
                minAngle = angle;
                minPoint = newViewCast.point;
            }
            else
            {
                maxAngle = angle;
                maxPoint = newViewCast.point;
            }
        }

        return new EdgeInfo(minPoint, maxPoint);

    }



    //Helper "class" to calculate direction form single rotational value.
    public Vector3 DirFromAngle(float angleDegrees, bool angleIsGlobal)
    {
        //Add player rotation to angle if needed
        if(!angleIsGlobal)
            angleDegrees += transform.eulerAngles.y;

        //Calculate and return angle.
        return new Vector3(Mathf.Sin(angleDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleDegrees * Mathf.Deg2Rad));
    }


    public struct ViewCastInfo
    {
        public bool hit;
        public Vector3 point;
        public float dist;
        public float angle;

        public ViewCastInfo(bool _hit, Vector3 _point, float _dist, float _angle)
        {
            hit = _hit;
            point = _point;
            dist = _dist;
            angle = _angle;
        }
    }


    //Helper "class" finding edges.
    public struct EdgeInfo
    {
        public Vector3 pointA;
        public Vector3 pointB;


        public EdgeInfo(Vector3 _pointA, Vector3 _pointB)
        {
            pointA = _pointA;
            pointB = _pointB;
        }
    }


}
