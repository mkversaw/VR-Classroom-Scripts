using UnityEngine;
using ViveSR.anipal.Eye;
using System.Runtime.InteropServices;
using System.IO;
using System;
using System.Text.RegularExpressions;

/// <summary>
/// Example usage for eye tracking callback
/// Note: Callback runs on a separate thread to report at ~120hz.
/// Unity is not threadsafe and cannot call any UnityEngine api from within callback thread.
/// </summary>
public class viveEye120HZ : MonoBehaviour
{

	public int LengthOfRay = 25;
	[SerializeField] private LineRenderer GazeRayRenderer;
	[SerializeField] private Vector3 GazeDirectionCombined;

	[SerializeField] private string filepath = "D:\\Miles\\60HZData\\60data.csv";
	[SerializeField] private string callBackFilePath = "D:\\Miles\\120HZData\\120data.csv";
	[SerializeField] private string eventFilePath = "D:\\Miles\\eventData\\eventData.csv";

	private float startTime;
	private float currentTime = 0;

	private static float currentTime3 = 0;

	private bool eyesFailed;

	[SerializeField] private GameObject screen;

	[SerializeField] private GameObject objectwithheatmap;

	[SerializeField] private GameObject objectwithlog;

	[SerializeField] private GameObject objectWithCone;

	private static EyeData eyeData = new EyeData();
	private static bool eye_callback_registered = false;
	
	private static StreamWriter writer;
	private static StreamWriter eventWriter;
	private static StreamWriter callBackWriter;
	
	private static bool hasName;

	private float callBackUpdateSpeed = 0;
	private static float callBackTimeStamp2 = 0;
	private static float callBackLastTime, currentTime2;


    private void Start()
    {
		callBackTimeStamp2 = 0;
		startTime = Time.time;
		VisionDataPreamble(filepath);
		CallBackVisionDataPreamble(callBackFilePath);
		EventDataPreamble(eventFilePath);
	}

    private void Update()
	{
		callBackUpdateSpeed = currentTime2 - callBackLastTime;
		callBackTimeStamp2 += callBackUpdateSpeed;
		// currentTime3 += Time.deltaTime;
		//Debug.Log("timeStamp: " + callBackTimeStamp2.ToString() + " ms");

		if (SRanipal_Eye_Framework.Status != SRanipal_Eye_Framework.FrameworkStatus.WORKING) return;

		if (SRanipal_Eye_Framework.Instance.EnableEyeDataCallback == true && eye_callback_registered == false)
		{
			SRanipal_Eye.WrapperRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye.CallbackBasic)EyeCallback));
			eye_callback_registered = true;
		}
		else if (SRanipal_Eye_Framework.Instance.EnableEyeDataCallback == false && eye_callback_registered == true)
		{
			SRanipal_Eye.WrapperUnRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye.CallbackBasic)EyeCallback));
			eye_callback_registered = false;
		}

		Vector3 GazeOriginCombinedLocal, GazeDirectionCombinedLocal;

		if (eye_callback_registered)
		{
			if (SRanipal_Eye.GetGazeRay(GazeIndex.COMBINE, out GazeOriginCombinedLocal, out GazeDirectionCombinedLocal, eyeData)) { }
			else if (SRanipal_Eye.GetGazeRay(GazeIndex.LEFT, out GazeOriginCombinedLocal, out GazeDirectionCombinedLocal, eyeData)) { }
			else if (SRanipal_Eye.GetGazeRay(GazeIndex.RIGHT, out GazeOriginCombinedLocal, out GazeDirectionCombinedLocal, eyeData)) { }
			else
			{
				VisionDataWriter2(false);
				return;
			}
		}
		else
		{
			if (SRanipal_Eye.GetGazeRay(GazeIndex.COMBINE, out GazeOriginCombinedLocal, out GazeDirectionCombinedLocal)) { }
			else if (SRanipal_Eye.GetGazeRay(GazeIndex.LEFT, out GazeOriginCombinedLocal, out GazeDirectionCombinedLocal)) { }
			else if (SRanipal_Eye.GetGazeRay(GazeIndex.RIGHT, out GazeOriginCombinedLocal, out GazeDirectionCombinedLocal)) { }
			else
			{
				VisionDataWriter2(false);
				return;
			}
		}


		GazeDirectionCombined = Camera.main.transform.TransformDirection(GazeDirectionCombinedLocal);
		GazeRayRenderer.SetPosition(0, Camera.main.transform.position - Camera.main.transform.up * 0.05f);
		GazeRayRenderer.SetPosition(1, Camera.main.transform.position + GazeDirectionCombined * LengthOfRay);

		VisionDataWriter2(true);

	}

	private void OnDisable()
	{
		Release();
	}

	void OnApplicationQuit()
	{
		Release();
		writer.Close();
		callBackWriter.Close();
		eventWriter.Close();
	}

	/// <summary>
	/// Release callback thread when disabled or quit
	/// </summary>
	private static void Release()
	{
		if (eye_callback_registered == true)
		{
			SRanipal_Eye.WrapperUnRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye.CallbackBasic)EyeCallback));
			eye_callback_registered = false;
		}
	}

	/// <summary>
	/// Required class for IL2CPP scripting backend support
	/// </summary>
	internal class MonoPInvokeCallbackAttribute : System.Attribute
	{
		public MonoPInvokeCallbackAttribute() { }
	}

	/// <summary>
	/// Eye tracking data callback thread.
	/// Reports data at ~120hz
	/// MonoPInvokeCallback attribute required for IL2CPP scripting backend
	/// </summary>
	/// <param name="eye_data">Reference to latest eye_data</param>
	[MonoPInvokeCallback]

	private static String quotifier2(String str)
	{
		return "\"" + str + "\"";
	}

	private static void EyeCallback(ref EyeData eye_data)
	{
		
		eyeData = eye_data;
		callBackLastTime = currentTime2;
		currentTime2 = eyeData.timestamp;

		// do stuff with eyeData...

		VerboseData dataObj = eyeData.verbose_data;
		SingleEyeData dataLeftEye = dataObj.left;
		SingleEyeData dataRightEye = dataObj.right;

		Vector3 gaze_origin_mm_LEFT = dataLeftEye.gaze_origin_mm;
		Vector3 gaze_direction_normalized_LEFT = dataLeftEye.gaze_direction_normalized;
		float pupil_diameter_mm_LEFT = dataLeftEye.pupil_diameter_mm;
		float eye_openness_LEFT = dataLeftEye.eye_openness;
		Vector2 pupil_position_in_sensor_area_LEFT = dataLeftEye.pupil_position_in_sensor_area;


		Vector3 gaze_origin_mm_RIGHT = dataRightEye.gaze_origin_mm;
		Vector3 gaze_direction_normalized_RIGHT = dataRightEye.gaze_direction_normalized;
		float pupil_diameter_mm_RIGHT = dataRightEye.pupil_diameter_mm;
		float eye_openness_RIGHT = dataRightEye.eye_openness;
		Vector2 pupil_position_in_sensor_area_RIGHT = dataRightEye.pupil_position_in_sensor_area;

		// String title = "LEFT: gaze origin (mm),gaze direction normalized (0 to 1),pupil diameter (mm),eye_openness,pupil position in sensor area (0 to 1),RIGHT: gaze origin (mm),gaze direction normalized (0 to 1),pupil diameter (mm),eye_openness,pupil position in sensor area (0 to 1)";

		String strLeft = quotifier2(gaze_origin_mm_LEFT.ToString()) + "," + quotifier2(gaze_direction_normalized_LEFT.ToString()) + "," + pupil_diameter_mm_LEFT.ToString() + "," + eye_openness_LEFT.ToString() + "," + quotifier2(pupil_position_in_sensor_area_LEFT.ToString());

		String strRight = quotifier2(gaze_origin_mm_RIGHT.ToString()) + "," + quotifier2(gaze_direction_normalized_RIGHT.ToString()) + "," + pupil_diameter_mm_RIGHT.ToString() + "," + eye_openness_RIGHT.ToString() + "," + quotifier2(pupil_position_in_sensor_area_RIGHT.ToString());

		String combinedData = callBackTimeStamp2.ToString() + "," + strLeft + "," + strRight;


		callBackWriter.WriteLine(combinedData);
	}

	private static void VisionDataPreamble(string filepath)
	{
		var digits = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
		while (System.IO.File.Exists(filepath))
		{

			string[] parsedFile = filepath.Split('.'); // result should be two strings, the file name , and the file type

			String result = Regex.Match(parsedFile[0], @"\d+$").Value; // check end of file name for a number

			if (String.IsNullOrEmpty(result)) // if empty no number was found
			{
				parsedFile[0] += "1"; // add 1 as the first number
				filepath = parsedFile[0] + "." + parsedFile[1]; // recombine the split strings to form the new filepath
			}
			else
			{
				int numValue = Int32.Parse(result); // convert the (string) number to an int
				numValue += 1; // increment the number by 1
				parsedFile[0] = parsedFile[0].TrimEnd(digits); // remove the number from the end
				parsedFile[0] += numValue.ToString(); // add the new number
				filepath = parsedFile[0] + "." + parsedFile[1];
			}

		}

		string titleLine = "Time,Gaze Position,Camera Position,Head Rotation,Hit Object Name,Hit Position,Peripheral hitlist";


		writer = new StreamWriter(filepath, true);
		writer.WriteLine(titleLine);
		writer.WriteLine();
	}


	private static void CallBackVisionDataPreamble(string callBackFilePath)
	{
		var digits = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
		while (System.IO.File.Exists(callBackFilePath))
		{

			string[] parsedFile = callBackFilePath.Split('.'); // result should be two strings, the file name , and the file type

			String result = Regex.Match(parsedFile[0], @"\d+$").Value; // check end of file name for a number

			if (String.IsNullOrEmpty(result)) // if empty no number was found
			{
				parsedFile[0] += "1"; // add 1 as the first number
				callBackFilePath = parsedFile[0] + "." + parsedFile[1]; // recombine the split strings to form the new filepath
			}
			else
			{
				int numValue = Int32.Parse(result); // convert the (string) number to an int
				numValue += 1; // increment the number by 1
				parsedFile[0] = parsedFile[0].TrimEnd(digits); // remove the number from the end
				parsedFile[0] += numValue.ToString(); // add the new number
				callBackFilePath = parsedFile[0] + "." + parsedFile[1];
			}

		}

		String titleLine = "Time,LEFT: gaze origin (mm),gaze direction normalized (0 to 1),pupil diameter (mm),eye_openness,pupil position in sensor area (0 to 1),RIGHT: gaze origin (mm),gaze direction normalized (0 to 1),pupil diameter (mm),eye_openness,pupil position in sensor area (0 to 1)";


		callBackWriter = new StreamWriter(callBackFilePath, true);
		callBackWriter.WriteLine(titleLine);
		callBackWriter.WriteLine();
	}

	String quotifier(String str)
	{
		return "\"" + str + "\"";
	}

	private static void EventDataPreamble(string eventFilePath)
	{
		var digits = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
		while (System.IO.File.Exists(eventFilePath))
		{

			string[] parsedFile = eventFilePath.Split('.'); // result should be two strings, the file name , and the file type

			String result = Regex.Match(parsedFile[0], @"\d+$").Value; // check end of file name for a number

			if (String.IsNullOrEmpty(result)) // if empty no number was found
			{
				parsedFile[0] += "1"; // add 1 as the first number
				eventFilePath = parsedFile[0] + "." + parsedFile[1]; // recombine the split strings to form the new filepath
			}
			else
			{
				int numValue = Int32.Parse(result); // convert the (string) number to an int
				numValue += 1; // increment the number by 1
				parsedFile[0] = parsedFile[0].TrimEnd(digits); // remove the number from the end
				parsedFile[0] += numValue.ToString(); // add the new number
				eventFilePath = parsedFile[0] + "." + parsedFile[1];
			}

		}

		string titleLine = "Time,Event";

		eventWriter = new StreamWriter(eventFilePath, true);
		eventWriter.WriteLine(titleLine);
		eventWriter.WriteLine();
	}

	public void EventDataWriter(GameObject obj) {
		string time = callBackTimeStamp2.ToString();
		eventWriter.WriteLine(time + "," + obj.name);
	}

	private void VisionDataWriter2(bool eyeEnabled)
	{
		RaycastHit hit;
		// string time = currentTime.ToString();
		string time = callBackTimeStamp2.ToString();
		if (!eyeEnabled)
		{
			writer.WriteLine(time + "," + "" + "," + "" + "," + "" + "," + "" + "," + ""); // write even if eye isnt found, but leave it all blank
			return; // don't need the rest
		}

		string eyeLook = GazeDirectionCombined.ToString();
		string hitPosition = "";
		string hitObjectName = "";
		string headRotation = Camera.main.transform.rotation.ToString();

		string cameraPosition = Camera.main.transform.position.ToString();

		if (Physics.Raycast(Camera.main.transform.position, GazeDirectionCombined, out hit))
		{
			objectwithlog.GetComponent<viveLookLog>().updateList(hit, hit.collider.gameObject.name);
			if (hit.collider.gameObject.tag == "Screen") // if its the screen then update the heat map
			{
				objectwithheatmap.GetComponent<viveHeatMap>().updatePoint(hit.point.x, hit.point.y);
			}

			hitPosition = hit.point.ToString();
			hitObjectName = hit.collider.gameObject.name.ToString();

		}

		string peripheryHitList = objectWithCone.GetComponent<viveVisionCone>().generateConeString(); // obtain all the objects in the peripheral vision from the cone

		writer.WriteLine(time + "," + quotifier(eyeLook) + "," + quotifier(cameraPosition) + "," + quotifier(headRotation) + "," + hitObjectName + "," + quotifier(hitPosition) + "," + peripheryHitList);
	}

}