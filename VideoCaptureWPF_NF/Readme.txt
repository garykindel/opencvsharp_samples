Sources:
https://blog.hompus.nl/2021/01/04/using-the-opencv-videocapture-class-with-the-name-of-a-camera/    - Get List of cameras
https://github.com/shimat/opencvsharp_samples => https://github.com/garykindel/opencvsharp_samples  - OpenCV sample

Nuget packages
Install-Package Hompus.VideoInputDevices -Version 0.5.0
Install-Package OpenCvSharp4 -Version 4.6.0.20220608
Install-Package OpenCvSharp4.WpfExtensions -Version 4.6.0.20220608

Copied the following files  (from VideoCaptureWPF example)
haarcascade_frontalface_default.xml not used script for finding face in video stream
x86 - OpenCvSharpExtern.dll and opencv_videoio_ffmpeg460.dll  C++ resources

