using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//struct ParticleData
//{
//	public float t;
//	//position along the curve
//	public float angle;
//	//position along the circle at given point in the curve
//	public float r;
//	public Vector3 prevPosition;
//	public Color color;
//}

/// <summary>
/// See CurrentStream. This was the original class
/// </summary>
public class Stream : MonoBehaviour
{
	private List<DataStreamNode> nodes;
	private DataStreamNode first;
	private ParticleSystem ps;
	private ParticleSystem.Particle[] particles;
	private ParticleData[] particleData;
  
	public Color ParticleColor;
	public float Radius = 1.0f;
	public float ParticleEnergy = 5.0f;
	public float ParticleTime = 5.0f;
	public float SurfaceVariation = 0.0f;
	public int NumParticles = 500;
  
	Vector3CatmullRomSpline positionSpline;

	public Vector3CatmullRomSpline Spline {
		get { return positionSpline; }
	}

	void Start ()
	{   
		InitSpline ();    
		InitParticles ();
	}

	void InitSpline ()
	{
		DataStreamNode[] allNodes = gameObject.GetComponentsInChildren<DataStreamNode> (); 
		foreach (DataStreamNode anyNode in allNodes) {
			if (anyNode.IsFirst) {
				first = anyNode;
				break;
			}
		}       
	  
		nodes = new List<DataStreamNode> ();
		positionSpline = new Vector3CatmullRomSpline ();    
	  
		DataStreamNode node = first;
		while (node != null) { 
			nodes.Add (node);
			positionSpline.Add (node.transform.position); 
			node = node.next;
		}
	}

	void InitParticles ()
	{
	
		this.ps = this.gameObject.AddComponent<ParticleSystem> () as ParticleSystem;
		this.ps.Stop ();
		Material pointMaterial = Resources.Load ("PointRGB", typeof(Material)) as Material;
		this.GetComponent<ParticleSystemRenderer> ().material = pointMaterial;
		this.particles = new ParticleSystem.Particle[NumParticles];        
	  
		particleData = new ParticleData[particles.Length];
	  
		for (int i = 0; i < particles.Length; i++) { 
			particleData [i].t = Random.value;       
			float a = Random.value;
			particleData [i].angle = a * Mathf.PI * 2.0f;  
			particleData [i].r = 1.0f + (Random.value - 0.5f) * SurfaceVariation;
			particleData [i].prevPosition = positionSpline.GetPosition (particleData [i].t);
			particleData [i].color = new Color (a, a, a);
		}
	}

	void Update ()
	{	  
    
		Vector3 lightPos = (new Vector3 (50, 50, 50)).normalized;
		for (int i = 0; i < particles.Length; i++) {     
			particleData [i].t += Time.deltaTime / ParticleTime; 
			if (particleData [i].t <= 0) {
				particleData [i].t = 0;
			}
			if (particleData [i].t > 1.0f) {
				particleData [i].t -= 1.0f; 
				particleData [i].prevPosition = positionSpline.GetPosition (particleData [i].t); 
			}               
			Vector3 positionOnCurve = positionSpline.GetPosition (particleData [i].t);
			Vector3 positionOnSurface = GetPositionOnSurface (particleData [i].t, particleData [i].angle, Radius * particleData [i].r);
			Vector3 normal = (positionOnSurface - positionOnCurve).normalized;
			//float d = 0.25f + 0.75f*Mathf.Max(0.0f, Vector3.Dot(normal, lightPos)); //that's the proper version
			float d = 0.5f + 0.5f * Vector3.Dot (normal, lightPos); //this looks cooler
			particles [i].position = positionOnSurface;
			particles [i].velocity = (particles [i].position - particleData [i].prevPosition).normalized;
			//particles[i].color = particleData[i].color;  

			float a = 1.0f;
			if (particleData [i].t < 0.2f) {
				a = particleData [i].t / 0.2f;
			} else if (particleData [i].t > 0.8f) {
				a = 1.0f - (particleData [i].t - 0.8f) / 0.2f;
			}
			particles [i].startColor = new Color (d, d, d, a);
			particles [i].startColor = new Color (255, 0, 0, 255);
			particleData [i].angle += 0.5f * Time.deltaTime;
			particleData [i].prevPosition = particles [i].position;
		}
		this.ps.SetParticles (particles, particles.Length);

	}

	void OnDrawGizmos ()
	{  
		DrawSpline ();  
	}

	void DrawSpline ()
	{    
		if (positionSpline == null)
			return;
		Gizmos.color = new Color (0.0f, 0.6f, 0.99f, 1.0f);
		Vector3 prevPos = positionSpline.GetPosition (0);
		int numSteps = 50;
		float step = 1.0f / numSteps;
		float t = step;  
		Gizmos.DrawWireSphere (prevPos, 0.25f);
		for (int j = 0; j < numSteps; j++, t += step) {
			Vector3 pos = positionSpline.GetPosition (t);
			Gizmos.DrawWireSphere (pos, 0.25f);
			Gizmos.DrawLine (prevPos, pos);     
			prevPos = pos;
		}
	}

	Vector3 GetPositionOnSurface (float t, float angle, float r)
	{
		Vector3 pos = positionSpline.GetPosition (t);  
		Vector3 prevPos = positionSpline.GetPosition (t - 0.01f);  
		Vector3 forward = pos - prevPos;
		Vector3 up = Vector3.up;
		Vector3 right = Vector3.Cross (forward, up).normalized;
		//reassign proper up
		up = Vector3.Cross (right, forward).normalized;    
		return pos + r * Mathf.Cos (angle) * right + r * Mathf.Sin (angle) * up;
	}
}
