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
using System.ServiceModel;

public class EmguTest : MonoBehaviour
{
	enum BlurType{Gauss, Median, Mean};

	Point zeroPoint = new Point(0, 0);

	Size rectSizeV;

	BlurType blurType = BlurType.Mean;
	VideoCapture webcam;
	WebCamDevice[] devices;

	int meanSize = 50;
	int medianSize = 7;
	int gaussSize = 3;
	double gaussSigma = 0.2;

	Size gaussSizeV;
	Size meanSizeV;

	int crossSize = 3;
	int iterations = 1;
	Size crossSizeV;

	int minFaceSize = 50;
	int maxFaceSize = 500;

	Size minFaceSizeV;
	Size maxFaceSizeV;

	Point centerPrevious;
	Point centerOfScreen;

	double hMin = 0;
	double hMax = 150;
	double sMin = 200;
	double sMax = 250;
	double vMin = 130;
	double vMax = 250;

	int areaStep = 1000;

	bool move = false;
	bool walk = false;
	bool attack = false;
	int faceUndetectCount = 0;
	int faceUndetectMax = 5;

	bool processing = true;

	RawImage img;
	Texture2D texture;
	Mat imgRGB = new Mat();
	Mat imgGray2 = new Mat();
	Mat imgTexture = new Mat ();
	Mat imgGray = new Mat();
	Mat imgHSV = new Mat();
	Mat imgHier = new Mat();
	Mat imgCross = new Mat ();
	Hsv lower;
	Hsv high;
	Image<Hsv, byte> imm;

	RectTransform movObj;

	VectorOfVectorOfPoint contours;
	Rectangle[] rects;
	Point anchor;
	MCvScalar contourColor;
	MCvScalar centerColor;

	CascadeClassifier classif;

	Animator dragonAnim;
	Transform dragon;
	Transform particles;

	float deltaX = 0;
	float deltaY = 0;

	string streamingAssetPath;

	void Awake ()
	{
		// init variables
		gaussSizeV = new Size (gaussSize, gaussSize);
		meanSizeV = new Size (meanSize, meanSize);

		crossSizeV = new Size (crossSize, crossSize);

		minFaceSizeV = new Size (minFaceSize, minFaceSize);
		maxFaceSizeV = new Size (maxFaceSize, maxFaceSize);

		lower = new Hsv (hMin, sMin, vMin);
		high = new Hsv (hMax, sMax, vMax);

		contours = new VectorOfVectorOfPoint();
		anchor = new Point ((int)(crossSize / 2) + 1, (int)(crossSize / 2) + 1);
		contourColor = new MCvScalar (0, 180, 0);
		centerColor = new MCvScalar (180, 180, 0);

		centerPrevious = new Point(-1, -1);
		centerOfScreen = new Point(Screen.width / 2, Screen.height / 2);

		img = GameObject.Find ("FireImageCam").GetComponent<RawImage> ();
		dragon = GameObject.Find ("Dragon").transform;
		dragonAnim = dragon.GetComponent<Animator> ();
		particles = GameObject.Find ("Canon_part").transform;
		devices = WebCamTexture.devices;

		streamingAssetPath = Application.dataPath;
		//classif = new CascadeClassifier (streamingAssetPath + "/data/lbpcascades/lbpcascade_frontalface_improved.xml");
		classif = new CascadeClassifier (streamingAssetPath + "/data/lbpcascades/lbpcascade_frontalcatface.xml");
	}

	// Use this for initialization
	void Start ()
	{
		// stop fire at time 0
		particles.GetComponent<ParticleSystem> ().Stop ();

		movObj = GameObject.Find ("FireImageCam").GetComponent<RectTransform> ();
		texture = new Texture2D ((int) movObj.rect.width, (int) movObj.rect.height, TextureFormat.RGBA32, false);
		rectSizeV = new Size ((int)movObj.rect.width, (int)movObj.rect.height);
	}

	// Update is called once per frame
	void Update ()
	{
		// give the raw image the cam texture
		if (!processing && webcam != null)
		{
			img.texture = texture;
			ImageToTexture (imgTexture, texture);
		}

		// if attack variable is different than the current state, change the state
		if (attack != dragonAnim.GetBool ("Attack"))
		{
			dragonAnim.SetBool ("Attack", attack);
		}

		if(move)
		{
			deltaX = centerPrevious.X - centerOfScreen.X;
			deltaY = -(centerPrevious.Y - centerOfScreen.Y); // negative because the Y axis starts at bottom

			// rotate with X axis difference
			dragon.rotation = Quaternion.Euler
			(
				new Vector3 (
					dragon.rotation.eulerAngles.x,
					dragon.rotation.eulerAngles.y + deltaX * Time.deltaTime * 0.5f,
					dragon.rotation.eulerAngles.z
				)
			);

			// translate with Y axis difference
			if (Mathf.Abs (deltaY) > 0.1f)
			{
				dragon.GetComponent<CharacterController>().Move(dragon.forward * deltaY * Time.deltaTime);
				if (!walk)
				{
					walk = true;
					dragonAnim.SetBool ("Walk", true);
				}
			}
			else
			{
				if (walk)
				{
					walk = false;
					dragonAnim.SetBool ("Walk", false);
				}
			}
		}
		else
		{
			if (walk)
			{
				walk = false;
				dragonAnim.SetBool ("Walk", false);
			}
		}
	}

	// calculate the center of the object with the color chosen, and detect cat face
	void ImageGrabbed(object sender, EventArgs e)
	{
		if (webcam != null && webcam.IsOpened)
		{
			if (webcam.IsOpened)
				webcam.Retrieve (imgRGB);
			if (imgRGB.IsEmpty)
				return;
			
			CvInvoke.Flip (imgRGB, imgRGB, FlipType.Horizontal);
			CvInvoke.CvtColor (imgRGB, imgGray, ColorConversion.Bgr2Gray);
			CvInvoke.CvtColor (imgRGB, imgHSV, ColorConversion.Bgr2Hsv);

			if (blurType == BlurType.Median)
			{
				CvInvoke.MedianBlur (imgHSV, imgHSV, medianSize);
			}
			else if (blurType == BlurType.Gauss)
			{
				CvInvoke.GaussianBlur (imgHSV, imgHSV, gaussSizeV, gaussSigma);
			}
			else if (blurType == BlurType.Mean)
			{
				CvInvoke.Blur (imgHSV, imgHSV, meanSizeV, zeroPoint);
			}

			imm = imgHSV.ToImage<Hsv, byte> ();

			imgGray2 = imm.InRange (lower, high).Mat;

			// clean impurities
			if (iterations != 0 && crossSize >= 3)
			{
				imgCross = CvInvoke.GetStructuringElement (ElementShape.Cross, crossSizeV, anchor);

				CvInvoke.Erode (imgGray2, imgGray2, imgCross, anchor, iterations, BorderType.Default, new MCvScalar (1));
				CvInvoke.Dilate (imgGray2, imgGray2, imgCross, anchor, iterations, BorderType.Default, new MCvScalar (1));
			}
			CvInvoke.FindContours (imgGray2, contours, imgHier, RetrType.List, ChainApproxMethod.ChainApproxNone);

			// get biggest area
			double biggestArea = 0;
			int biggestIndex = -1;
			for (int i = 0; i < contours.Size; i++)
			{
				double area = CvInvoke.ContourArea (contours [i]);

				if (area > biggestArea)
				{
					biggestArea = area;
					biggestIndex = i;
				}
			}

			int centerX = -1;
			int centerY = -1;
			if (biggestIndex >= 0)
			{
				MCvMoments centerM = CvInvoke.Moments (contours [biggestIndex]);
				centerX = (int)(centerM.M10 / centerM.M00);
				centerY = (int)(centerM.M01 / centerM.M00);
			}

			// a big enough area means we must move
			if (biggestArea > areaStep)
			{
				move = true;
			}
			else
			{
				move = false;
			}

			// if no center found, set it to the center of the screen, to get a delta = 0
			if (centerX != -1 && centerY != -1)
			{
				centerPrevious.X = centerX;
				centerPrevious.Y = centerY;
				// draw the center of the area as green circle
				CvInvoke.Circle (imgRGB, centerPrevious, 20, contourColor, 15);
			}
			else
			{
				centerPrevious = centerOfScreen;
			}
			// draw the center of the screen as blue circle
			CvInvoke.Circle(imgRGB, centerOfScreen, 20, centerColor, 15);

			// detect cat face
			rects = classif.DetectMultiScale (imgGray, 1.1, 5, minFaceSizeV, maxFaceSizeV);
			processing = false;

			// draw a green rectangle where we found a cat face
			for (int i = 0; i < rects.Length; i++)
			{
				CvInvoke.Rectangle (imgRGB, rects [i], contourColor, 15);
			}

			// if there is at least 1 cat face=> ATTACK!!!
			if (rects.Length > 0)
			{
				faceUndetectCount = 0;
				attack = true;
			}
			else
			{
				faceUndetectCount++;
				if (faceUndetectCount >= faceUndetectMax)
				{
					attack = false;
				}
			}

			// resize the image and flip it in order to convert it later and put it into raw texture
			CvInvoke.Resize (imgRGB, imgTexture, rectSizeV);
			CvInvoke.Flip (imgTexture, imgTexture, FlipType.Vertical);
		}
		GC.Collect ();
	}

	void OnGUI()
	{
		// if no camera selected, choose one from the available
		if (webcam == null)
		{
			for (int i = 0; i < devices.Length; i++)
			{
				if (devices [i].isFrontFacing)
				{
					if (GUI.Button (new Rect (new Vector2 (20, 20 + 50 * i), new Vector2 (300, 30)), devices [i].name + "(" + (devices [i].isFrontFacing ? "Front" : "Back") + ")"))
					{
						webcam = new VideoCapture (i);

						centerOfScreen = new Point(webcam.Width / 2, webcam.Height / 2);

						webcam.ImageGrabbed += ImageGrabbed;
						webcam.Start ();
					}
				}
			}
		}
		else
		{
			/*/
			meanSize = int.Parse(GUI.TextField(new Rect (new Vector2 (340 + 20, 20 + 50), new Vector2 (100, 30)), meanSize.ToString()));
			if (GUI.Button (new Rect (new Vector2 (20, 20 + 50), new Vector2 (300, 30)), "Moyenne"))
			{
				blurType = BlurType.Mean;
			}
			medianSize = int.Parse(GUI.TextField(new Rect (new Vector2 (340 + 20, 20 + 100), new Vector2 (100, 30)), medianSize.ToString()));
			if (GUI.Button (new Rect (new Vector2 (20, 20 + 100), new Vector2 (300, 30)), "Mediane"))
			{
				blurType = BlurType.Median;
			}
			gaussSize = int.Parse(GUI.TextField(new Rect (new Vector2 (340 + 20, 20 + 150), new Vector2 (100, 30)), gaussSize.ToString()));
			gaussSigma = double.Parse(GUI.TextField(new Rect (new Vector2 (480 + 20, 20 + 150), new Vector2 (100, 30)), gaussSigma.ToString()));
			if (GUI.Button (new Rect (new Vector2 (20, 20 + 150), new Vector2 (300, 30)), "Gaussienne"))
			{
				blurType = BlurType.Gauss;
			}
			crossSize = int.Parse(GUI.TextField(new Rect (new Vector2 (640 + 20, 20 + 50), new Vector2 (100, 30)), crossSize.ToString()));
			iterations = int.Parse(GUI.TextField(new Rect (new Vector2 (640 + 20, 20 + 100), new Vector2 (100, 30)), iterations.ToString()));
			//*/

			/*/
			hMin = double.Parse(GUI.TextField(new Rect (new Vector2 (760 + 20, 20 + 050), new Vector2 (100, 30)), hMin.ToString()));
			hMax = double.Parse(GUI.TextField(new Rect (new Vector2 (760 + 20, 20 + 100), new Vector2 (100, 30)), hMax.ToString()));
			sMin = double.Parse(GUI.TextField(new Rect (new Vector2 (760 + 20, 20 + 150), new Vector2 (100, 30)), sMin.ToString()));
			sMax = double.Parse(GUI.TextField(new Rect (new Vector2 (760 + 20, 20 + 200), new Vector2 (100, 30)), sMax.ToString()));
			vMin = double.Parse(GUI.TextField(new Rect (new Vector2 (760 + 20, 20 + 250), new Vector2 (100, 30)), vMin.ToString()));
			vMax = double.Parse(GUI.TextField(new Rect (new Vector2 (760 + 20, 20 + 300), new Vector2 (100, 30)), vMax.ToString()));
			//*/
		}
	}

	void OnDestroy()
	{
		Debug.Log ("killing cam!");
		if (webcam != null)
		{
			webcam.Stop ();
			webcam.Dispose ();
		}
	}

	public void ImageToTexture(Mat image, Texture2D texture)
	{
		// convert image to texture
		texture.LoadRawTextureData(image.ToImage<Rgba, byte> ().Bytes);
		texture.Apply ();
	}
}
