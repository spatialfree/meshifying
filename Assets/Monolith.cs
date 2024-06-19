using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using EzySlice;
using GK;
using TMPro;

public class Monolith : MonoBehaviour
{
  public float holdTime = 5;
  public Vector3 slicePos;
  public float angle, sliceDist;
  public List<GameObject> toSlice, sliced;

  [Header("References")]
  public GameObject go;
  public Transform slicePlane, held;
  public LineRenderer line;
  public Material matSlice;
  public Mesh meshBox;
  public TextMeshPro text;

  ConvexHullCalculator convexCalc = new ConvexHullCalculator();
  Mesh newMesh;
  List<Vector3> verts = new List<Vector3>();
  List<int> tris = new List<int>();
  List<Vector3> normals = new List<Vector3>();

  void Start()
  {
    newMesh = new Mesh();
    text.text = "swipe";
  }

  Vector3 pressPos, mousePos;
  float pressTime, flash;
  bool slow;
  void Update()
  {
    mousePos = Input.mousePosition;
    mousePos.x /= Screen.width;
    mousePos.y /= Screen.height;
    mousePos.x -= 0.5f;
    mousePos.y -= 0.5f;
    mousePos *= 6;

    if (Input.GetMouseButtonDown(0))
    {
      pressPos = mousePos;
      pressTime = Time.time;

      slow = mousePos.magnitude > 1;
    }

    if (Input.GetMouseButtonUp(0))
    {
      if (Time.time - pressTime < 1 && Vector3.Distance(mousePos, pressPos) < 1f)
      {
        for (int i = 0; i < toSlice.Count; i++)
        {
          Rigidbody rb = toSlice[i].GetComponent<Rigidbody>();
          rb.AddExplosionForce(2, mousePos + Vector3.forward * -0.2f, 1, 0, ForceMode.Impulse);
        }
      }
      slow = false;
    }

    if (Input.GetMouseButton(0) && Vector3.Distance(mousePos, pressPos) > 1f)
    {
      line.SetPosition(0, pressPos);
      line.SetPosition(1, mousePos);
    }
    else
    {
      line.SetPosition(0, Vector3.zero);
      line.SetPosition(1, Vector3.zero);
    }

    if (Input.GetMouseButtonUp(0) && Time.time - pressTime < holdTime)
    {
      // slice direction
      Vector3 swipeDir = Quaternion.Euler(0, 0, 90) * (mousePos - pressPos).normalized;
      slicePos = Vector3.Lerp(mousePos, pressPos, 0.5f);
      sliceDist = Vector3.Distance(mousePos, pressPos);

      if (sliceDist > 1f)
      {
        if (toSlice.Count < 50)
        {
          slicePlane.position = slicePos;
          slicePlane.rotation = Quaternion.LookRotation(Vector3.forward, swipeDir);

          Slice(slicePos, swipeDir);
          text.text = "";
          slicePlane.localScale = new Vector3(sliceDist, 1, 1);
        }
        else
        {
          text.text = "HOLD";
        }
      }
    }

    slicePlane.localScale *= 1 - Time.deltaTime;

    flash *= 1 - Time.deltaTime;
    Shader.SetGlobalFloat("flash", flash);

    if (!Input.GetMouseButton(0) || mousePos.magnitude > 1 || pressPos.magnitude > 1) { pressTime = Time.time; }

    held.localScale = Vector3.one * 2 * Mathf.Clamp01((Time.time - pressTime) / holdTime);
  }

  bool slowSwap;
  void FixedUpdate()
  {
    if (Input.GetMouseButton(0) && pressPos.magnitude < 1 && mousePos.magnitude < 1)
    {
      int inside = 0;
      for (int i = 0; i < toSlice.Count; i++)
      {
        Rigidbody rb = toSlice[i].GetComponent<Rigidbody>();
        if (rb.position.magnitude > 0.8f)
        {
          rb.AddForce(-rb.position);
          Quaternion rot = Quaternion.LookRotation(rb.position);
          Vector3 vel = rot * rb.velocity;
          vel.x *= 1 - Time.fixedDeltaTime * 3;
          vel.y *= 1 - Time.fixedDeltaTime * 3;
          rb.velocity = Quaternion.Inverse(rot) * vel;
        }
        else
        {
          inside++;
        }
      }

      if (inside > 2 && Time.fixedTime - pressTime > holdTime)
      {
        float maxDist = 0;
        Vector3 vertCenter = Vector3.zero;
        List<Vector3> points = new List<Vector3>();
        for (int i = toSlice.Count - 1; i >= 0; i--)
        {
          GameObject ts = toSlice[i];
          Mesh mesh = ts.GetComponent<MeshFilter>().mesh;
          Vector3 meshCenter = Vector3.zero;
          for (int v = 0; v < mesh.vertices.Length; v++)
          {
            Vector3 vert = ts.transform.TransformPoint(mesh.vertices[v]);
            meshCenter += vert;
          }

          if (Vector2.Distance(meshCenter / mesh.vertices.Length, Vector3.zero) < 0.8f)
          {
            for (int v = 0; v < mesh.vertices.Length; v++)
            {
              Vector3 vert = ts.transform.TransformPoint(mesh.vertices[v]);
              vertCenter += vert;
              points.Add(vert);
            }

            ts.SetActive(false);
            sliced.Add(ts);
            toSlice.RemoveAt(i);
          }
        }


        vertCenter = vertCenter / points.Count;
        for (int i = points.Count - 1; i >= 0; i--)
        {
          points[i] -= vertCenter;
          float vertMag = points[i].magnitude;
          if (vertMag > maxDist)
          {
            maxDist = vertMag;
          }
        }

        for (int i = points.Count - 1; i >= 0; i--)
        {
          points[i] = points[i] / maxDist;
          points[i] += vertCenter;
        }

        for (int i = points.Count - 1; i >= 0; i--)
        {
          for (int j = points.Count - 1; j >= 0; j--)
          {
            if (i != j && Vector3.Distance(points[i], points[j]) < 0.1f)
            {
              points.RemoveAt(i);
              break;
            }
          }
        }

        convexCalc.GenerateHull(points, true, ref verts, ref tris, ref normals);
        newMesh = new Mesh();
        if (verts.Count == tris.Count && verts.Count == normals.Count)
        {
          newMesh.SetVertices(verts);
          newMesh.SetTriangles(tris, 0);
          newMesh.SetNormals(normals);
          // newMesh.SetUVs(0, Unwrapping.GeneratePerTriangleUV(newMesh));
        }
        else
        {
          newMesh = Instantiate(meshBox);
        }

        GameObject newObj = Instantiate(go);

        newObj.AddComponent<MeshFilter>().mesh = newMesh;
        newObj.AddComponent<MeshCollider>().convex = true;
        // .sharedMesh = newMesh;
        toSlice.Add(newObj);
        flash = 1.5f;
        pressTime = Time.time;
        text.text = "";
      }
    }

    if (slowSwap != slow)
    {
      for (int i = 0; i < toSlice.Count; i++)
      {
        Rigidbody rb = toSlice[i].GetComponent<Rigidbody>();
        if (slow)
        {
          rb.velocity = rb.velocity / 4;
        }
        else
        {
          rb.velocity *= 4;
        }
      }
      slowSwap = slow;
    }
  }

  public void Slice(Vector3 planeWorldPosition, Vector3 planeWorldDirection)
  {
    for (int i = toSlice.Count - 1; i >= 0; i--)
    {
      GameObject ts = toSlice[i];
      if (Vector2.Distance(ts.transform.position, slicePos) < sliceDist)
      {
        GameObject[] s = ts.SliceInstantiate(planeWorldPosition, planeWorldDirection);
        if (s != null)
        {
          foreach (GameObject o in s)
          {
            o.AddComponent<MeshCollider>().convex = true;
            Rigidbody rb = o.AddComponent<Rigidbody>();
            // rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.useGravity = false;
            rb.angularDrag = 1;
            rb.constraints = RigidbodyConstraints.FreezePositionZ;
            rb.AddExplosionForce(0.02f, slicePos + Vector3.forward * -0.2f, sliceDist / 3, 0, ForceMode.Impulse);

            MeshRenderer rend = o.GetComponent<MeshRenderer>();
            Material[] mats = rend.materials;
            for (int m = 1; m < mats.Length; m++)
            {
              mats[m] = matSlice;
            }
            rend.materials = mats;

            toSlice.Add(o);
          }

          ts.SetActive(false);
          sliced.Add(ts);
          toSlice.RemoveAt(i);
        }
      }
    }
  }
}

