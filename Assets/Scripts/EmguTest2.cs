using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Emgu.CV;
using Emgu.CV.Util;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System.IO;
using System;
using ZedGraph;
using System.Drawing;
using UnityEngine.UI;

// NOT USED
public class EmguTest2 : MonoBehaviour
{
	VideoCapture webcam;
	VideoWriter writer;

	int minFaceSize = 50;
	int maxFaceSize = 200;

	RawImage img1;
	Texture2D texture1;
	MemoryStream m;

	void Awake ()
	{
		img1 = GameObject.Find ("ImageCam").GetComponent<RawImage> ();
	}

	// Use this for initialization
	void Start ()
	{
		GameObject.Find ("Canon_part").GetComponent<ParticleSystem> ().Stop ();
		RectTransform movObj = GameObject.Find ("ImageCam").GetComponent<RectTransform> ();
		texture1 = new Texture2D ((int) movObj.rect.width, (int) movObj.rect.height);
		webcam.Start ();
	}

	// Update is called once per frame
	void Update ()
	{
		if (webcam != null && webcam.IsOpened)
		{
			//webcam.Grab ();
		}
	}

	void OnGUI()
	{
		if (webcam == null)
		{
			WebCamDevice[] devices = WebCamTexture.devices;

			for (int i = 0; i < devices.Length; i++)
			{
				if (devices [i].isFrontFacing)
				{
					if (GUI.Button (new Rect (new Vector2 (20, 20 + 50 * i), new Vector2 (300, 30)), devices [i].name + "(" + (devices [i].isFrontFacing ? "Front" : "Back") + ")"))
					{
						webcam = new VideoCapture (i);
						writer = new VideoWriter ("D:\\bla.mp4", 30, new Size (webcam.Width, webcam.Height), true);

						webcam.ImageGrabbed += new EventHandler(ImageGrabbed);
						break;
					}
				}
			}
		}
		else
		{
			minFaceSize = int.Parse(GUI.TextField(new Rect (new Vector2 (640 + 20, 20 + 50), new Vector2 (100, 30)), minFaceSize.ToString()));
			maxFaceSize = int.Parse(GUI.TextField(new Rect (new Vector2 (640 + 20, 20 + 100), new Vector2 (100, 30)), maxFaceSize.ToString()));
		}
	}

	void OnDestroy()
	{
		CvInvoke.DestroyAllWindows ();
		if(writer != null) writer.Dispose ();
		if(webcam != null) webcam.Stop ();
	}

	void ImageGrabbed(object sender, EventArgs e)
	{
		Mat imgRGB = new Mat();
		Mat imgGray = new Mat();
		Mat imgHSV = new Mat();

		if (webcam.IsOpened)
			webcam.Retrieve (imgRGB);
		if (imgRGB.IsEmpty)
			return;

		CvInvoke.Flip (imgRGB, imgRGB, FlipType.Horizontal);

		CvInvoke.CvtColor (imgRGB, imgGray, ColorConversion.Bgr2Gray);
		CvInvoke.CvtColor (imgRGB, imgHSV, ColorConversion.Bgr2Hsv);

		CascadeClassifier classif = new CascadeClassifier (Application.streamingAssetsPath + "/../data/lbpcascades/lbpcascade_frontalface_improved.xml");

		Rectangle[] rects = classif.DetectMultiScale (imgGray, 1.1, 5, new Size (minFaceSize, minFaceSize), new Size (maxFaceSize, maxFaceSize));

		for (int i = 0; i < rects.Length; i++)
		{
			CvInvoke.Rectangle (imgRGB, rects [i], new MCvScalar (0, 180, 0), 5);
		}

		if (rects.Length > 0)
		{
			GameObject.Find ("Canon_part").GetComponent<ParticleSystem> ().Play ();
		}
		else
		{
			GameObject.Find ("Canon_part").GetComponent<ParticleSystem> ().Stop ();
		}

		/*CvInvoke.Imshow ("ImageRGB", imgRGB);
		CvInvoke.Imshow ("ImageGray", imgGray);
		CvInvoke.Imshow ("ImageHSV", imgHSV);*/

		ImageToTexture (imgRGB, texture1);
		img1.texture = texture1;
	}

	public void ImageToTexture(Mat image, Texture2D texture)
	{
		m = new MemoryStream();
		image.Bitmap.Save(m, image.Bitmap.RawFormat);
		texture.LoadImage(m.ToArray());
	}
}
