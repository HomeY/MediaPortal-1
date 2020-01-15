#region Copyright (C) 2005-2020 Team MediaPortal

// Copyright (C) 2005-2020 Team MediaPortal
// http://www.team-mediaportal.com
// 
// MediaPortal is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// MediaPortal is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with MediaPortal. If not, see <http://www.gnu.org/licenses/>.

#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Diagnostics;
using System.Linq;

using MediaPortal.GUI.Library;
using MediaPortal.Util;

using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Iptc;
using MetadataExtractor.Formats.Xmp;
using MetadataExtractor.Formats.Exif.Makernotes;

namespace MediaPortal.GUI.Pictures
{
  #region Exif Read Routines

  public class ExifMetadata : IDisposable
  {
    public ExifMetadata() {}

    public void Dispose() {}

    public struct MetadataItem
    {
      public string Caption;
      public string Tag;
      public string Name;
      public string Value;
      public string DisplayValue;
    }

    public struct Metadata
    {
      public MetadataItem ViewerComments;
      public MetadataItem EquipmentMake;
      public MetadataItem CameraModel;
      public MetadataItem ExposureTime;
      public MetadataItem Fstop;
      public MetadataItem DatePictureTaken;
      public MetadataItem ShutterSpeed;
      public MetadataItem ExposureCompensation;
      public MetadataItem MeteringMode;
      public MetadataItem Flash;
      public MetadataItem Resolution;
      public MetadataItem ImageDimensions;
      public MetadataItem Orientation;
      public MetadataItem ISO;
      public MetadataItem Author;
      public MetadataItem Copyright;
      public MetadataItem ExposureProgram;
      public MetadataItem ExposureMode;
      public MetadataItem SensingMethod;
      public MetadataItem SceneType;
      public MetadataItem SceneCaptureType;
      public MetadataItem WhiteBalance;
      public MetadataItem Lens;
      public MetadataItem FocalLength;
      public MetadataItem FocalLength35MM;
      public MetadataItem Comment;
      public MetadataItem CountryCode;
      public MetadataItem CountryName;
      public MetadataItem ProvinceOrState;
      public MetadataItem City;
      public MetadataItem SubLocation;
      public MetadataItem Keywords;
      public MetadataItem ByLine;
      public MetadataItem CopyrightNotice;
      public MetadataItem Latitude;
      public MetadataItem Longitude;
      public MetadataItem Altitude;
    }

    public int Count()
    {
      return 37; // TODO fix hard code later
    }

    private void SetStuff(ref MetadataItem item, Directory directory, int tag)
    {
      try
      {
        item.Tag = tag.ToString("X");
        item.Caption = tag.ToExifString() ?? string.Empty;
        item.Name = directory.GetTagName(tag);
        item.DisplayValue = directory.GetDescription(tag);
        item.Value = string.Empty;

        switch (tag)
        {
          case ExifDirectoryBase.TagOrientation:
          case ExifDirectoryBase.TagMeteringMode:
          case ExifDirectoryBase.TagFlash:
          {
            if (directory.TryGetInt32(tag, out var intValue))
            {
              item.Value = intValue.ToString();
            }
            else
            {
              item.Value = "0";
            }
            break;
          }
          case ExifDirectoryBase.TagDateTime:
          case ExifDirectoryBase.TagDateTimeOriginal:
          {
            if (directory.TryGetDateTime(tag, out var dateTime))
            {
              item.Value = dateTime.ToString("yyyy-MM-dd HH:mm:ss");
              item.DisplayValue = dateTime.ToString(System.Threading.Thread.CurrentThread.CurrentCulture);
            }
            break;
          }
          case ExifDirectoryBase.TagLensModel:
          {
            string lensMake = directory.GetDescription(ExifDirectoryBase.TagLensMake);
            if (!string.IsNullOrEmpty(lensMake))
            {
              item.Value = lensMake;
            }
            break;
          }
          case IptcDirectory.TagKeywords:
          {
            string keywords = string.Empty;
            var keywordsArray = directory.GetStringArray(tag);
            if (keywordsArray != null)
            {
              foreach (string keyword in keywordsArray)
              {
                keywords += keyword + "; ";
              }
            }
            if (!string.IsNullOrEmpty(keywords))
            {
              item.DisplayValue = keywords;
            }
            break;
          }
          case GpsDirectory.TagLatitude:
          {
            string latitudeRef = directory.GetDescription(GpsDirectory.TagLatitudeRef);
            if (!string.IsNullOrEmpty(latitudeRef))
            {
              item.DisplayValue = latitudeRef + " " +item.DisplayValue;
            }
            break;
          }
          case GpsDirectory.TagLongitude:
          {
            string longitudeRef = directory.GetDescription(GpsDirectory.TagLongitudeRef);
            if (!string.IsNullOrEmpty(longitudeRef))
            {
              item.DisplayValue = longitudeRef + " " +item.DisplayValue;
            }
            break;
          }
          case GpsDirectory.TagAltitude:
          {
            string altitudeRef = directory.GetDescription(GpsDirectory.TagAltitudeRef);
            if (!string.IsNullOrEmpty(altitudeRef))
            {
              item.DisplayValue = altitudeRef + " " +item.DisplayValue;
            }
            break;
          }
        }

        if (item.Tag == "unknown")
        {
          item.Tag = string.Empty;
        }
        if (item.Caption == "unknown")
        {
          item.Caption = string.Empty;
        }
        if (item.Name == "unknown")
        {
          item.Name = string.Empty;
        }
        if (item.DisplayValue == "unknown")
        {
          item.DisplayValue = string.Empty;
        }
        if (item.Value == "unknown")
        {
          item.Value = string.Empty;
        }
      }
      catch (Exception) { }
    }

    public Metadata GetExifMetadata(string photoName)
    {
      // Create an instance of Metadata 
      Metadata MyMetadata = new Metadata();

      try
      {
        // Read EXIF Data
        IEnumerable<Directory> directories = ImageMetadataReader.ReadMetadata(photoName);
        var exifDirectory = directories.OfType<ExifIfd0Directory>().FirstOrDefault();

        if (exifDirectory != null)
        {
          // DateTime
          SetStuff(ref MyMetadata.DatePictureTaken, exifDirectory, ExifDirectoryBase.TagDateTime);

          // [Exif IFD0] Make: Equipment Make: NIKON CORPORATION
          SetStuff(ref MyMetadata.EquipmentMake, exifDirectory, ExifDirectoryBase.TagMake);

          // [Exif IFD0] Model: Camera Model: NIKON D90
          SetStuff(ref MyMetadata.CameraModel, exifDirectory, ExifDirectoryBase.TagModel);

          // [Exif IFD0] Artist: Author: Andrew J.Swan 
          SetStuff(ref MyMetadata.Author, exifDirectory, ExifDirectoryBase.TagArtist);

          // [Exif IFD0] Software: ApplicationName: Adobe Photoshop CC (Windows)
          SetStuff(ref MyMetadata.ViewerComments, exifDirectory, ExifDirectoryBase.TagSoftware);

          // [Exif IFD0] Copyright: Copyright: Copyright (c) 2014 by Andrew J.Swan
          SetStuff(ref MyMetadata.Copyright, exifDirectory, ExifDirectoryBase.TagCopyright);

          // [Exif IFD0] Orientation: Orientation: 1 - Normal
          SetStuff(ref MyMetadata.Orientation, exifDirectory, ExifDirectoryBase.TagOrientation);

          // [Exif IFD0] Resolution Unit: Resolution Unit: 2 - Inch
          // = exifDirectory.GetDescription(ExifDirectoryBase.TagResolutionUnit);
        }

        foreach (var subIfdDirectory in directories.OfType<ExifSubIfdDirectory>())
        {
          if (subIfdDirectory != null)
          {
            // [Exif SubIFD] Date/Time Original: Date Picture Taken: 23/11/2014 17:09:41
            string dateTime = subIfdDirectory.GetDescription(ExifDirectoryBase.TagDateTimeOriginal);
            if (string.IsNullOrEmpty(dateTime))
            {
              continue;
            }
            SetStuff(ref MyMetadata.DatePictureTaken, subIfdDirectory, ExifDirectoryBase.TagDateTimeOriginal);

            // [Exif SubIFD] ISO Speed Ratings: ISO: 200
            SetStuff(ref MyMetadata.ISO, subIfdDirectory, ExifDirectoryBase.TagIsoEquivalent);

            // [Exif SubIFD] Metering Mode: Metering Mode: 5 - Multi-segment
            SetStuff(ref MyMetadata.MeteringMode, subIfdDirectory, ExifDirectoryBase.TagMeteringMode);

            // [Exif SubIFD] Flash: Flash: 15 - Flash fired, compulsory flash mode, return light detected
            SetStuff(ref MyMetadata.Flash, subIfdDirectory, ExifDirectoryBase.TagFlash);

            // [Exif SubIFD] Exposure Time: Exposure Time: 1/60(s)
            SetStuff(ref MyMetadata.ExposureTime, subIfdDirectory, ExifDirectoryBase.TagExposureTime);

            // [Exif SubIFD] Exposure Program: Exposure Program: 2 - Normal program
            SetStuff(ref MyMetadata.ExposureProgram, subIfdDirectory, ExifDirectoryBase.TagExposureProgram);

            // [Exif SubIFD] Exposure Mode:
            SetStuff(ref MyMetadata.ExposureMode, subIfdDirectory, ExifDirectoryBase.TagExposureMode);

            // [Exif SubIFD] Exposure Bias Value: Exposure Compensation: 0/6
            SetStuff(ref MyMetadata.ExposureCompensation, subIfdDirectory, ExifDirectoryBase.TagExposureBias);

            // [Exif SubIFD] F-Number: FStop: F4
            SetStuff(ref MyMetadata.Fstop, subIfdDirectory, ExifDirectoryBase.TagFNumber);

            // [Exif SubIFD] Shutter Speed Value: Shutter Speed: 5,906891 (5906891/1000000)
            SetStuff(ref MyMetadata.ShutterSpeed, subIfdDirectory, ExifDirectoryBase.TagShutterSpeed);

            // [Exif SubIFD] Sensing Method: 
            SetStuff(ref MyMetadata.SensingMethod, subIfdDirectory, ExifDirectoryBase.TagSensingMethod);

            // [Exif SubIFD] Scene Type:
            SetStuff(ref MyMetadata.SceneType, subIfdDirectory, ExifDirectoryBase.TagSceneType);

            // [Exif SubIFD] Scene Capture Type:
            SetStuff(ref MyMetadata.SceneCaptureType, subIfdDirectory, ExifDirectoryBase.TagSceneCaptureType);

            // [Exif SubIFD] White Balance Mode:
            SetStuff(ref MyMetadata.WhiteBalance, subIfdDirectory, ExifDirectoryBase.TagWhiteBalanceMode);

            // [Exif SubIFD] Lens Make: NIKON
            // [Exif SubIFD] Lens Model: Lens Model: 35.0 mm f/1.8
            SetStuff(ref MyMetadata.Lens, subIfdDirectory, ExifDirectoryBase.TagLensModel);

            // [Exif SubIFD] Focal Length: Focal Length: 35 (350/10)
            SetStuff(ref MyMetadata.FocalLength, subIfdDirectory, ExifDirectoryBase.TagFocalLength);

            // [Exif SubIFD] Focal Length 35: Focal Length (35mm film): 52
            SetStuff(ref MyMetadata.FocalLength35MM, subIfdDirectory, ExifDirectoryBase.Tag35MMFilmEquivFocalLength);

            // [Exif SubIFD] User Comment: Comment: Copyright (C) by Andrew J.Swan
            SetStuff(ref MyMetadata.Comment, subIfdDirectory, ExifDirectoryBase.TagUserComment);
          }
        }

        GetMakerNoteLens(ref MyMetadata.Lens, directories) ;

        var iptcDirectory = directories.OfType<IptcDirectory>().FirstOrDefault();
        if (iptcDirectory != null)
        {
          // [IPTC] Country/Primary Location Code:
          SetStuff(ref MyMetadata.CountryCode, iptcDirectory, IptcDirectory.TagCountryOrPrimaryLocationCode);

          // [IPTC] Country/Primary Location Name: Country: �������
          SetStuff(ref MyMetadata.CountryName, iptcDirectory, IptcDirectory.TagCountryOrPrimaryLocationName);

          // [IPTC] Province/State: State: ����
          SetStuff(ref MyMetadata.ProvinceOrState, iptcDirectory, IptcDirectory.TagProvinceOrState);

          // [IPTC] City: City: ����
          SetStuff(ref MyMetadata.City, iptcDirectory, IptcDirectory.TagCity);

          // [IPTC] Sub-location: Sublocation: ������� �������
          SetStuff(ref MyMetadata.SubLocation, iptcDirectory, IptcDirectory.TagSubLocation);

          // [IPTC] Keywords: Keywords: 2014, UKR, �������������, ����, ��������, ���������, �������
          SetStuff(ref MyMetadata.Keywords, iptcDirectory, IptcDirectory.TagKeywords);

          // [IPTC] By-line: Jason P. Odell
          SetStuff(ref MyMetadata.ByLine, iptcDirectory, IptcDirectory.TagByLine);

          // [IPTC] Copyright Notice: � Jason P. Odell
          SetStuff(ref MyMetadata.CopyrightNotice, iptcDirectory, IptcDirectory.TagCopyrightNotice);

          // [IPTC] Caption/Abstract: For Educational Use Only
          // string captionAbstract = iptcDirectory.GetDescription(IptcDirectory.TagCaption);
        }

        var gpsDirectory = directories.OfType<GpsDirectory>().FirstOrDefault();
        if (gpsDirectory != null)
        {
          // [GPS] GPS Latitude Ref: + [GPS] GPS Latitude:
          SetStuff(ref MyMetadata.Latitude, gpsDirectory, GpsDirectory.TagLatitude);

          // [GPS] GPS Longitude Ref: + [GPS] GPS Longitude:
          SetStuff(ref MyMetadata.Longitude, gpsDirectory, GpsDirectory.TagLongitude);

          // [GPS] GPS Altitude Ref: + [GPS] GPS Altitude:
          SetStuff(ref MyMetadata.Altitude, gpsDirectory, GpsDirectory.TagAltitude);
        }

        // Create an instance of the image to gather metadata from 
        using (Image MyImage = Image.FromFile(photoName))
        {
          MyMetadata.Resolution.DisplayValue = MyImage.HorizontalResolution.ToString() + "x" + MyImage.VerticalResolution.ToString();
          MyMetadata.Resolution.Caption = "Resolution ";

          MyMetadata.ImageDimensions.DisplayValue = MyImage.Width.ToString() + "x" + MyImage.Height.ToString();
          MyMetadata.ImageDimensions.Caption = "Dimensions";
        }
      }
      catch (Exception ex)
      {
        Log.Error("ExifExtractor: GetExifMetadata {0}", ex.Message);
      }
      return MyMetadata;
    }
    
    private void GetMakerNoteLens(ref MetadataItem item, IEnumerable<Directory> directory)
    {
      if (!string.IsNullOrEmpty(item.DisplayValue))
      {
        return;
      }

      string lensModel = string.Empty;

      var xmpDirectory = directory.OfType<XmpDirectory>().FirstOrDefault();
      if (xmpDirectory != null)
      {
        var xmpDictionary = xmpDirectory.GetXmpProperties();
        // [XMPMeta] Lens: 18.0-105.0 mm f/3.5-5.6
        if (xmpDictionary.TryGetValue("aux:Lens", out lensModel))
        {
          item.DisplayValue = lensModel;
          return;
        }
      }

      foreach (var _directory in directory)
      {
        if (_directory.Name.Contains("Nikon Makernote"))
        {
          // [Nikon Makernote] Lens: 18.0-105.0 mm f/3.5-5.6
          lensModel = _directory.GetDescription(NikonType2MakernoteDirectory.TagLens);
          if (!string.IsNullOrEmpty(lensModel))
          {
            item.DisplayValue = lensModel;
            return;
          }
        }
      }
    }
  }

  #endregion
}