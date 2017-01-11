using UnityEngine;
using System.Collections;
using System.IO;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

[RequireComponent(typeof(RawImage))]
public class ReadPrintMaster : MonoBehaviour {
    // Copy these two files from the Printmaster DOS disk into Assets/ folder
    const string NAME_FILE = "STANDARD.SDR";
    const string DATA_FILE = "STANDARD.SHP";

    // Printshop and Printmaster are ALMOST identical, just use these files and toggle the boolean
    // const string NAME_FILE = "GRAPHICS.PNM";
    // const string DATA_FILE = "GRAPHICS.PNG"; // not a "real" png file - let's hope Unity can cope with it
    const bool isPrintshopFormat = false; 

    // IMPORTANT: These are the pixel dimensions, but a screen aspect ratio of 88/52 is expected
    // on a square pixel LCD monitor it would look stretched!
    // We are cheating and just doubling the number of rows for the export.
    const int WIDTH = 88;
    const int HEIGHT = 52;

    int imageCount;
    List<string> imageNames = new List<string>();
    int currImageNum = 0;
    Texture2D tex;
    Color[] pixels;
    byte[] bytes;


    // Use this for initialization
    void Start() {
        ReadNames();
        ReadData();
        ShowImage(0);
    }

    void ReadNames()
    {
        const int CHAR_COUNT = 16;

        var path = Path.Combine(Application.dataPath, NAME_FILE);
        var bytes = File.ReadAllBytes(path);
        imageCount = bytes.Length / CHAR_COUNT;

        for (int i = 0; i < bytes.Length; i += CHAR_COUNT)
        {
            var name = System.Text.Encoding.UTF8.GetString(bytes, i, CHAR_COUNT).TrimEnd('\0');
            imageNames.Add(name);
        }

    }

    void WriteToFile(byte[] bytes, string filename)
    {
        filename = filename.Replace('?', 'q');
        filename = filename.Replace('!', 'e');
        var path = Path.Combine(Application.dataPath, "Output");
        path = Path.Combine(path, filename+".png");
        File.WriteAllBytes(path, bytes);
    }

    const int WIDTH_SCALE = 2;
    const int HEIGHT_SCALE = 3;

    void ReadData()
    {
        var path = Path.Combine(Application.dataPath, DATA_FILE);
        bytes = File.ReadAllBytes(path);
        tex = new Texture2D(WIDTH * WIDTH_SCALE, HEIGHT * HEIGHT_SCALE);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        GetComponent<RawImage>().texture = tex;
    }

    void ShowImage(int img)
    {
        currImageNum = img;
        const int MAGIC_SIZE = isPrintshopFormat ? 10 : 0; // printshop only has a 10 byte file header
        const int HEADER_SIZE = isPrintshopFormat ? 0 : 4; // 4 bytes per image for printmaster
        const int TAIL_SIZE = isPrintshopFormat ? 0 : 1;
        const int IMAGE_SIZE = WIDTH * HEIGHT / 8; // 8 pixels per byte

        var ptr = img * (HEADER_SIZE + TAIL_SIZE + IMAGE_SIZE) + HEADER_SIZE + MAGIC_SIZE;

        BitArray arr = new BitArray(bytes, ptr, IMAGE_SIZE);
        for (int y = 0; y < HEIGHT; ++y)
        {
            for (int x = 0; x < WIDTH; ++x)
            {
                var flippedY = HEIGHT - 1 - y;
                Color col = arr.Get(flippedY * WIDTH + x) ? Color.black : Color.white;
                for (int yMul = 0; yMul < HEIGHT_SCALE; ++yMul)
                {
                    for (int xMul = 0; xMul < WIDTH_SCALE; ++xMul)
                    {
                        // aspect ratio cheat
                        tex.SetPixel(x * WIDTH_SCALE + xMul, y * HEIGHT_SCALE + yMul, col);
                    }
                }                
            }
        }
        tex.Apply();
    }

    void WriteCurrImage()
    {
        WriteToFile(tex.EncodeToPNG(), imageNames[(int)currImageNum]);
    }

    public void OnGUI()
    {
        if (GUI.Button(new Rect(0, 0, 100, 100), "<"))
        {
            if (--currImageNum < 0)
                currImageNum = imageCount-1;
            ShowImage((int)currImageNum);
        }
        if (GUI.Button(new Rect(100, 0, 100, 100), ">"))
        {
            if (++currImageNum >= imageCount)
                currImageNum = 0;
            ShowImage((int)currImageNum);
        }
        if (GUI.Button(new Rect(200, 0, 100, 100), "Export All"))
        {
            for (int img = 0; img < this.imageCount; ++img)
            {
                ShowImage(img);
                WriteCurrImage();
            }
        }
    }

    public class BitArray
    {
        static int size = sizeof(byte) * 8;
        byte[] data;

        public byte[] Data
        {
            get
            {
                return data;
            }
        }

        public BitArray(byte[] data, int offset, int length)
        {
            this.data = new byte[length];
            Array.Copy(data, offset, this.data, 0, length);
        }

        public int Length
        {
            get
            {
                return data.Length * size;
            }
        }

        public bool Get(int index)
        {
            var i = index / size;
            var b = index % size;
            var bitindex = 1 << (7 - b);//1 << b;
            return (data[i] & bitindex) > 0;
        }
    }

}
