using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Xml.Linq;
using System.Xml;
using OSGeo.GDAL;
using OSGeo.OSR;



namespace test64
{
    public partial class Form1 : Form
    {
        public Form1()
        {

            InitializeComponent();
            Gdal.AllRegister();
        }
        delegate void H5rd(object obj);
  
        enum Options
        {
            Sinusoidal,
            Equal_LatLon,
            Equal_LatLon_Interpolation
        }
        private void button1_Click(object sender, EventArgs e)
        {
            var fbd = new FolderBrowserDialog();
            fbd.Description = "init folder:\nL:\\Data_Test\\DPC_GF05\\20180527";
            fbd.SelectedPath = "L" + ":\\Data_Test\\DPC_GF05\\20180527";
            DialogResult result = fbd.ShowDialog();

            if (result == DialogResult.OK)
            {
                DirectoryInfo dir = new DirectoryInfo(fbd.SelectedPath);


                List<string> BlueFileList = new List<string>();
                string Bluepattern = "*B443.h5";
                FileInfo[] Bluefil = dir.GetFiles(Bluepattern);
                foreach (FileInfo f in Bluefil)
                {
                    BlueFileList.Add(f.FullName);
                }
             
                //Thread readThread = new Thread(new ParameterizedThreadStart(H5DataRead));          
                //readThread.IsBackground = true;
                //readThread.Start(BlueFileList[0]);
                //readThread.Join();
                //Thread readThread1 = new Thread(new ParameterizedThreadStart(H5DataRead));
                //readThread1.IsBackground = true;
                //readThread1.Start(BlueFileList[1]);
                
                //threadpool test
                //for (int i = 0; i < BlueFileList.Count-1; i++)
                //{
                //    ThreadPool.QueueUserWorkItem(task => { H5DataRead(BlueFileList[i]); }, i);
                //}

                //delegate test
                //H5rd rd1 = new H5rd(H5DataRead);
                //rd1(BlueFileList[0]);
                //H5rd rd2 = new H5rd(H5DataRead);
                //rd2(BlueFileList[1]);


                for (int i = 0; i < BlueFileList.Count; i++)
                {
                  
                    label1.Text = "正在处理第" + (i + 1).ToString() + "/" + BlueFileList.Count.ToString() + "轨数据";
                    Application.DoEvents();
                    //necessary,otherwise memory error
                    GC.Collect();
                    Thread.Sleep(100);
                    //must use delegate
                    H5rd rd = new H5rd(H5DataRead);
                    rd(BlueFileList[i]);
                 
                    //LabelDisp lbds = new LabelDisp(x=> { label1.Text = "正在处理第" + (i + 1).ToString() + "//" + BlueFileList.Count.ToString() + "轨数据"; });
                    //lbds(0);
                    //H5DataRead(BlueFileList[i]);
                }
                label1.Text = "图像生成完毕,右上角退出";
            }
        }
      
        
        void H5DataRead(object Filename)
        {
            
            PB_pro.Visible = true;
            PB_pro.Value = 0;
            PB_pro.Step = 10;
            //define parameters and dataset
           
            //file-->driver(not necessary)-->subdata-->band
          
                Dataset ds;
                Driver drv;
                Dataset Bluetmpdt;
                Band Bluetmpbd;
                Dataset Greends;
                Dataset Redds;
                Dataset Greentmpdt;
                Band Greenrsptmpbd;
                Dataset Redtmpdt;
                Band Redrsptmpbd;
                Dataset Lontmpdt;
                Band Lontmpbd;
                Bitmap bmp;
                Bitmap reprojbmp;

                string file = (string)Filename;
                string path = System.IO.Path.GetDirectoryName(file);
                string filename = System.IO.Path.GetFileName(file);

                ds = Gdal.Open(file, Access.GA_ReadOnly);
                drv = ds.GetDriver();
                string[] BlueSubdatas = ds.GetMetadata("SUBDATASETS");
                int subdata_num;
                DateTime dt = new DateTime();

                PB_pro.PerformStep();
                //read 1 band about 2.5 seconds [6084x12168]
                //0 for I490P   
                dt = DateTime.Now;
                subdata_num = 0;
                string tmpstr = BlueSubdatas[subdata_num];
                tmpstr = tmpstr.Substring(tmpstr.IndexOf("=") + 1);
         
                Bluetmpdt = Gdal.Open(tmpstr, Access.GA_ReadOnly);


                Bluetmpbd = Bluetmpdt.GetRasterBand(1);
                int Xsize = Bluetmpbd.XSize;
                int Ysize = Bluetmpbd.YSize;
                int resampleScale = 2;
                int rspXsize = Xsize / resampleScale;
                int rspYsize = Ysize / resampleScale;
          


                //green and red
                string GreenFilename = path + "\\" + filename.Substring(0, 37) + "B670.h5";
                //string GreenFilename = path + "\\" + filename.Substring(0, 37) + "B865.h5";
                string RedFilename = path + "\\" + filename.Substring(0, 37) + "B865.h5";
              

                Greends = Gdal.Open(GreenFilename, Access.GA_ReadOnly);

                Redds = Gdal.Open(RedFilename, Access.GA_ReadOnly);
                string[] GreenSubdatas = Greends.GetMetadata("SUBDATASETS");
                string[] RedSubdatas = Redds.GetMetadata("SUBDATASETS");

                string Greentmpstr = GreenSubdatas[subdata_num];
                string Redtmpstr = RedSubdatas[subdata_num];
                Greentmpstr = Greentmpstr.Substring(Greentmpstr.IndexOf("=") + 1);
                Redtmpstr = Redtmpstr.Substring(Redtmpstr.IndexOf("=") + 1);
                Greentmpdt = Gdal.Open(Greentmpstr, Access.GA_ReadOnly);

                Redtmpdt = Gdal.Open(Redtmpstr, Access.GA_ReadOnly);

                Greenrsptmpbd = Greentmpdt.GetRasterBand(1);
                Redrsptmpbd = Redtmpdt.GetRasterBand(1);
                PB_pro.PerformStep();
             


                short[] Bluedata;
                Bluedata = new short[rspXsize * rspYsize];
                Bluetmpbd.ReadRaster(0, 0, Xsize, Ysize, Bluedata, rspXsize, rspYsize, 0, 0);    

                short[] Greendata;
                Greendata = new short[rspXsize * rspYsize];
                Greenrsptmpbd.ReadRaster(0, 0, Xsize, Ysize, Greendata, rspXsize, rspYsize, 0, 0);

                short[] Reddata;
                Reddata = new short[rspXsize * rspYsize];
                Redrsptmpbd.ReadRaster(0, 0, Xsize, Ysize, Reddata, rspXsize, rspYsize, 0, 0);
                PB_pro.PerformStep();
                PB_pro.PerformStep();
                //dosomething...     

                //dat 
                subdata_num = 16;
                string Lontmpstr = BlueSubdatas[subdata_num];
                Lontmpstr = Lontmpstr.Substring(Lontmpstr.IndexOf("=") + 1);
                float[] lon = new float[rspXsize * rspYsize];
                Lontmpdt = Gdal.Open(Lontmpstr, Access.GA_ReadOnly);
                Lontmpbd = Lontmpdt.GetRasterBand(1);
                Lontmpbd.ReadRaster(0, 0, Xsize, Ysize, lon, rspXsize, rspYsize, 0, 0);
                float[] buffer = lon;
                FileStream vFileStream = new FileStream(@"C:\Users\QR\Desktop\gf\lon.dat",
                FileMode.Create, FileAccess.Write);
                byte[] temp = new byte[buffer.Length * sizeof(float)];
                Buffer.BlockCopy(buffer, 0, temp, 0, temp.Length);
                vFileStream.Write(temp, 0, temp.Length);
                vFileStream.Close();
                PB_pro.PerformStep();


                string jpgpath = "C:\\Users\\QR\\Desktop\\pol\\" + filename.Substring(0, 23) + ".jpg";
                string reproj_jpgpath = "C:\\Users\\QR\\Desktop\\polreproj\\" + filename.Substring(0, 23) + ".jpg";
                //bitmap max width and height :10587

             
                 
                 bmp = new Bitmap(rspXsize, rspYsize, PixelFormat.Format24bppRgb);
                 reprojbmp = new Bitmap(rspXsize, rspYsize, PixelFormat.Format24bppRgb);
                 SetBitmap(bmp, Reddata, Greendata, Bluedata, rspXsize, rspYsize, Options.Sinusoidal);
                 PB_pro.PerformStep();
                 SetBitmap(reprojbmp, Reddata, Greendata, Bluedata, rspXsize, rspYsize, Options.Equal_LatLon_Interpolation);
                 PB_pro.PerformStep();
                 PB_pro.PerformStep();
                 PB_pro.PerformStep();
            
                //Data = null;



                //save jpg 
                bmp.Save(jpgpath, System.Drawing.Imaging.ImageFormat.Jpeg);
                reprojbmp.Save(reproj_jpgpath, System.Drawing.Imaging.ImageFormat.Jpeg);




                //create spatial reference .jpw and auxiliary .xml
                string jpgfilename = System.IO.Path.GetFileName(jpgpath);
                string jpgfiledir = System.IO.Path.GetDirectoryName(jpgpath);
                string jpwFile = jpgfiledir + "\\" + jpgfilename.Substring(0, jpgfilename.Length - 1) + "w";
                string xmlFile = jpgfiledir + "\\" + jpgfilename + ".aux.xml";
             
                string reproj_jpgfilename = System.IO.Path.GetFileName(reproj_jpgpath);
                string reproj_jpgfiledir = System.IO.Path.GetDirectoryName(reproj_jpgpath);
                string reproj_jpwFile = reproj_jpgfiledir + "\\" + reproj_jpgfilename.Substring(0, reproj_jpgfilename.Length - 1) + "w";
                string reproj_xmlFile = reproj_jpgfiledir + "\\" + reproj_jpgfilename + ".aux.xml";

                Createjpw(jpwFile, rspXsize, rspYsize);
                CreateXml(xmlFile);
                Createjpw(reproj_jpwFile, rspXsize, rspYsize);
                CreateXml(reproj_xmlFile);
          
                //release memory    
                Bluedata = null;
                Greendata = null;
                Reddata = null;
                lon = null;
                buffer = null;

                Bluetmpbd.Dispose();
                Bluetmpdt.Dispose();
                Redrsptmpbd.Dispose();
                Redtmpdt.Dispose();
                Greenrsptmpbd.Dispose();
                Greentmpdt.Dispose();
                Lontmpdt.Dispose();
                Lontmpbd.Dispose();
                drv.Dispose();
                ds.Dispose();
                bmp.Dispose();
                reprojbmp.Dispose();
                //ClearMemory();
                PB_pro.Value = 100;          
                //MessageBox.Show("create jpg and jpw:" + (DateTime.Now - dt).ToString());
                PB_pro.Visible = false;
                
           
          
       

        }

        private void CreateXml(string path)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load("pattern.xml");
            xmlDoc.Save(path);
        }

        private void Createjpw(string path, double xsize, double ysize)
        {
            //calculate 1 pixel = ?? lat(lon)
            double Xunit = 360d / xsize;
            double Yunit = -180d / ysize;
            var fs = new System.IO.FileStream(path, FileMode.Create);
            var sw = new StreamWriter(fs);
            ///xResolution(1 pixel) 
            ///xRotation 
            ///yRotation 
            ///yResolution(1 pixel) 
            ///xOffset 
            ///yOffset
            sw.WriteLine(Xunit.ToString());
            sw.WriteLine("0");
            sw.WriteLine("0");
            sw.WriteLine(Yunit.ToString());
            //upperleft pixel centre
            sw.WriteLine(-180+Xunit/2);
            sw.WriteLine(90+Yunit/2);
            sw.Flush();
            //dispose
            sw.Dispose();
            fs.Dispose();

        }
        private void SetBitmap(Bitmap btmp, short[] Blue, short[] Green, short[] Red, int Xsize, int Ysize ,Options opt)
        {
            if (opt==Options.Sinusoidal)
            {
                for (int i = 0; i < Xsize; i++)
                    for (int j = 0; j < Ysize; j++)
                    {
                        int pos = j * Xsize + i;
                        if (Blue[pos] > 0 && Blue[pos] != 32767 && Green[pos] > 0 && Green[pos] != 32767 && Red[pos] > 0 && Red[pos] != 32767)
                        {
                            //data*3 makes image brighter(0~32767-->0~255)           
                            int blue = (int)(Blue[pos] + 1) / 128 * 3;
                            int green = (int)(Green[pos] + 1) / 128 * 3;
                            int red = (int)(Red[pos] + 1) / 128 * 3;
                            if (blue > 255)
                            {
                                blue = 255;
                            }
                            if (red > 255)
                            {
                                red = 255;
                            }
                            if (green > 255)
                            {
                                green = 255;
                            }
                            btmp.SetPixel(i, j, Color.FromArgb(blue, green, red));
                           
                        }

                    }
            }
            else
            {
                //FileStream vFileStreamlat = new FileStream(@"C:\Users\QR\Desktop\gf\lat.dat",
                //FileMode.Open, FileAccess.Read);
                //byte[] temp = new byte[vFileStreamlat.Length];
                //vFileStreamlat.Read(temp, 0, temp.Length);
                //float[] lat = new float[temp.Length / sizeof(float)];
                //Buffer.BlockCopy(temp, 0, lat, 0, lat.Length * sizeof(float));
                //vFileStreamlat.Close();

                FileStream vFileStreamlon = new FileStream(@"C:\Users\QR\Desktop\gf\lon.dat",
                 FileMode.Open, FileAccess.Read);
                byte[] temp1 = new byte[vFileStreamlon.Length];
                vFileStreamlon.Read(temp1, 0, temp1.Length);
                float[] lon = new float[temp1.Length / sizeof(float)];
                Buffer.BlockCopy(temp1, 0, lon, 0, lon.Length * sizeof(float));
                vFileStreamlon.Close();       
                    for (int i = 0; i < Xsize; i++)
                        for (int j = 0; j < Ysize; j++)
                        {
                            int pos = j * Xsize + i;
                            if (lon[pos] == -99999 || lon[pos] == 99999)
                            {
                                continue;
                            }
                            else
                            {
                            

                                int reprojX = (int)(lon[pos] * Xsize / 360f + Xsize / 2);

                                if (Blue[pos] > 0 && Blue[pos] != 32767 && Green[pos] > 0 && Green[pos] != 32767 && Red[pos] > 0 && Red[pos] != 32767)
                                {
                                    //data*3 makes image brighter(0~32767-->0~255)           
                                    int blue = (int)(Blue[pos] + 1) / 128 * 3;
                                    int green = (int)(Green[pos] + 1) / 128 * 3;
                                    int red = (int)(Red[pos] + 1) / 128 * 3;
                                    if (blue > 255)
                                    {
                                        blue = 255;
                                    }
                                    if (red > 255)
                                    {
                                        red = 255;
                                    }
                                    if (green > 255)
                                    {
                                        green = 255;
                                    }
                                    if (reprojX >=6084) continue;
                                    btmp.SetPixel(reprojX, j, Color.FromArgb(blue, green, red));
                                   
                                    //interpolation
                                    if (opt == Options.Equal_LatLon_Interpolation)
                                    {
                                        if (reprojX%Xsize!=0&&reprojX%Xsize!=Xsize-1&&btmp.GetPixel(reprojX - 1, j).R == 0)
                                        {
                                            btmp.SetPixel(reprojX - 1, j, Color.FromArgb(blue, green, red));
                                
                                        }
                                    }
                                }


                            }
                        }
                         
            }
            
        }
    }
}
