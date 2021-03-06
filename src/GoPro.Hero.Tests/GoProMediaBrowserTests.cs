﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using GoPro.Hero.Browser;
using GoPro.Hero.Browser.Media;
using GoPro.Hero.Hero3;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GoPro.Hero.Utilities;

namespace GoPro.Hero.Tests
{
    [TestClass]
    public class GoProMediaBrowserTests
    {
        private delegate IEnumerable<IMedia> ParseDelegate(Stream json);

        private MediaBrowser _instance;
        private ParseDelegate _parseDelegate;

        [TestInitialize]
        public void Initialize()
        {
            _instance = Activator.CreateInstance<GoProMediaBrowser>();

            (_instance as IGeneralBrowser).Initialize(null, null);

            var browserType = _instance.GetType();
            var method=browserType.GetMethod("Parse", BindingFlags.NonPublic | BindingFlags.Instance);
            _parseDelegate= (ParseDelegate)method.CreateDelegate(typeof(ParseDelegate), _instance);
        }

        [TestMethod]
        public void CheckContentsFromSample()
        {
            var sample = GetSample();
            var contents = _parseDelegate(sample);
            Assert.IsNotNull(contents);
        }


        [TestMethod]
        public void CheckContentsFromCamera()
        {
            var camera = GetCamera();
            var contents=camera.Browse<GoProMediaBrowser>().ContentsAsync().Result;
            Assert.IsNotNull(contents);
        }

        [TestMethod]
        public void CheckImagesFromCamera()
        {
            var camera = GetCamera();
            var first = camera.Contents().ContentsAsync<Image>().Result.ToList();
            var second = camera.Browse<GoProMediaBrowser>().ImagesAsync().Result.ToList();
            var third = camera.Contents().ImagesAsync().Result.ToList();
            var forth = camera.Browse<GoProMediaBrowser>().ContentsAsync<Image>().Result.ToList();

            
            CollectionAssert.AreEquivalent(first, second);
            CollectionAssert.AreEquivalent(third, forth);
            CollectionAssert.AreEquivalent(forth, first);

            if (first == null)
                Assert.Inconclusive("no image found");
        }

        [TestMethod]
        public void CheckTimeLapsesFromCamera()
        {
            var camera = GetCamera();
            var first = camera.Contents().ContentsAsync<TimeLapsedImage>().Result.ToList();
            var second = camera.Browse<GoProMediaBrowser>().TimeLapsesAsync().Result.ToList();
            var third = camera.Contents().TimeLapsesAsync().Result.ToList();
            var forth = camera.Browse<GoProMediaBrowser>().ContentsAsync<TimeLapsedImage>().Result.ToList();


            CollectionAssert.AreEquivalent(first, second);
            CollectionAssert.AreEquivalent(third, forth);
            CollectionAssert.AreEquivalent(forth, first);

            if (first == null)
                Assert.Inconclusive("no timelapsed image found");
        }

        [TestMethod]
        public void CheckDownloadImage()
        {
            var camera = GetCamera();
            var image=camera.Contents().ImagesAsync().Result.FirstOrDefault();
            if (image == null)
                Assert.Inconclusive("no image found");

            var response=image.DownloadAsync().Result.GetResponseStream();

            var memory = ReadToMemory(response);

            Assert.AreEqual(memory.Length, image.Size);
        }

        [TestMethod]
        public void CheckDownloadVideo()
        {
            var camera = GetCamera();
            var video = camera.Contents().VideosAsync().Result.FirstOrDefault();
            if (video == null)
                Assert.Inconclusive("no video found");

            var response = video.DownloadAsync().Result.GetResponseStream();

            var memory = ReadToMemory(response);

            Assert.AreEqual(memory.Length, video.Size);
        }

        [TestMethod]
        public void CheckDownloadVideoLowResolution()
        {
            var camera = GetCamera();
            var video = camera.Contents().VideosAsync().Result.Where(v => v.LowResolutionSize > 0).FirstOrDefault();
            if (video == null)
                Assert.Inconclusive("no video found");

            var response = video.DownloadLowResolutionAsync().Result.GetResponseStream();

            var memory = ReadToMemory(response);

            Assert.AreEqual(memory.Length, video.LowResolutionSize);
        }

        [TestMethod]
        public void CheckDownloadTimeLapsedImage()
        {
            var camera = GetCamera();
            var timeLapsed = camera.Contents().TimeLapsesAsync().Result.FirstOrDefault();
            if (timeLapsed == null)
                Assert.Inconclusive("no timelapsed image found");

            long size = 0;
            foreach (var item in timeLapsed)
            {
                var stream=item.GetResponseStream();
                var memory = ReadToMemory(stream);
                size += memory.Length;
            }

            Assert.AreEqual(size, timeLapsed.Size);
        }

        [TestMethod]
        public void CheckDownloadImageThumbnail()
        {
            var camera = GetCamera();
            var image = camera.Contents().ImagesAsync().Result.FirstOrDefault();
            var thumbnail=image.ThumbnailAsync().Result;
            var memory = ReadToMemory(thumbnail);
        }

        [TestMethod]
        public void CheckDownloadImageBigThumbnail()
        {
            var camera = GetCamera();
            var image = camera.Contents().ImagesAsync().Result.FirstOrDefault();
            var thumbnail = image.BigThumbnailAsync().Result;
            var memory = ReadToMemory(thumbnail);
        }

        [TestMethod]
        public void CheckDownloadTimeLapsedThumbnail()
        {
            var camera = GetCamera();
            var timeLapse = camera.Contents().TimeLapsesAsync().Result.FirstOrDefault();
            var thumbnail = timeLapse.ThumbnailAsync().Result;
            var memory = ReadToMemory(thumbnail);
            Assert.IsTrue(memory.Length > 1024);
        }

        [TestMethod]
        public void CheckDownloadTimeLapsedBigThumbnail()
        {
            var camera = GetCamera();
            var timeLapse = camera.Contents().TimeLapsesAsync().Result.FirstOrDefault();
            var thumbnail = timeLapse.BigThumbnailAsync().Result;
            var memory = ReadToMemory(thumbnail);
            Assert.IsTrue(memory.Length > 1024);
        }

        [TestMethod]
        public void CheckDownloadVideoThumbnail()
        {
            var camera = GetCamera();
            var video = camera.Contents().VideosAsync().Result.FirstOrDefault();
            var thumbnail = video.ThumbnailAsync().Result;
            var memory = ReadToMemory(thumbnail);
            Assert.IsTrue(memory.Length > 1024);
        }

        [TestMethod]
        public void CheckVideoInfo()
        {
            var camera = GetCamera();
            var video = camera.Contents().VideosAsync().Result.FirstOrDefault();
            var videoInfo = video.InfoAsync().Result;
            Assert.IsNotNull(videoInfo);
        }

        [TestMethod]
        public void CheckDeleteVideo()
        {
            var camera = GetCamera();
            var video = camera.Contents().VideosAsync().Result.FirstOrDefault();
            var deleted=video.DeleteAsync().Result.ContentAsync(video.Name).Result;
            Assert.IsNull(deleted);
        }

        [TestMethod]
        public void CheckDeleteImage()
        {
            var camera = GetCamera();
            var image = camera.Contents().ImagesAsync().Result.FirstOrDefault();
            var deleted = image.DeleteAsync().Result.ContentAsync(image.Name).Result;
            Assert.IsNull(deleted);
        }

        [TestMethod]
        public void CheckDeleteTimeLapse()
        {
            var camera = GetCamera();
            var timeLapse = camera.Contents().TimeLapsesAsync().Result.FirstOrDefault();
            var deleted = timeLapse.DeleteAsync().Result.ContentAsync(timeLapse.Name).Result;
            Assert.IsNull(deleted);
        }

        private MemoryStream ReadToMemory(Stream response)
        {
            var memory = new MemoryStream();
            var buffer = new byte[4096];
            while (true)
            {
                var readCount = response.Read(buffer, 0, buffer.Length);
                memory.Write(buffer, 0, readCount);
                if (readCount == 0) break;
            }
            return memory;
        }

        private Hero3Camera GetCamera()
        {
            return Camera.Create<Hero3Camera>(ExpectedParameters.IP_ADDRESS);
        }

        private Stream GetSample()
        {
            var sample =
                Assembly.GetExecutingAssembly().GetManifestResourceStream("GoPro.Hero.Tests.Resources.MediaBrowserSample.json");

            return sample;
        }
    }
}
