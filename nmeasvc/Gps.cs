using System;
using System.Device.Location;

namespace nmeasvc
{
    class Gps
    {
        private GeoCoordinateWatcher watcher;
        private Location location = new Location();
        private object lockGps = new object();

        public Gps()
        {
            Setup();
        }

        ~Gps()
        {
            Destroy();
        }

        private void Setup()
        {
            watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High);
            watcher.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(OnGpsPos);
            watcher.StatusChanged += new EventHandler<GeoPositionStatusChangedEventArgs>(OnGpsStatus);
            watcher.Start();
        }

        private void Destroy()
        {
            watcher.Stop();
            watcher.Dispose();
            watcher = null;
        }

        private void OnGpsPos(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            lock (lockGps)
            {
                if (!e.Position.Location.IsUnknown)
                {
                    location.lat = e.Position.Location.Latitude;
                    location.lon = e.Position.Location.Longitude;
                    location.alt = e.Position.Location.Altitude;
                    location.speed = e.Position.Location.Speed;
                    location.ha = e.Position.Location.HorizontalAccuracy;
                    location.va = e.Position.Location.VerticalAccuracy;
                    location.time = e.Position.Timestamp.ToUniversalTime();
                    location.localTime = DateTime.Now;
                }
            }
        }

        private void OnGpsStatus(object sender, GeoPositionStatusChangedEventArgs e)
        {
            if (e.Status == GeoPositionStatus.Disabled)
            {
                // needs proper testing, no idea if this works?
                Reboot();
            }
        }

        public void Reboot()
        {
            Destroy();
            Setup();
        }

        public Location GetLocation()
        {
            return location;
        }

        public string GetNmea()
        {
            var lat = NmeaCoord(location.lat, true);
            var lon = NmeaCoord(location.lon, false);
            var alt = location.alt.ToString("F1");
            var speed = (location.speed * 1.94384449).ToString("F3");
            var time = location.time.ToString("HHmmss.ff");
            var date = location.time.ToString("ddMMyy");

            var gga = string.Format("GPGGA,{0},{1},{2},1,12,,{3},M,,M,,,",
                time, lat, lon, alt);
            var gll = string.Format("GPGLL,{0},{1},{2},V",
                lat, lon, time);
            var rmc = string.Format("GPRMC,{0},A,{1},{2},{3},0,{4},0,0,A",
                time, lat, lon, speed, date);
            var gsa = string.Format("GPGSA,A,3,01,02,03,04,05,06,07,08,09,10,11,12,{0},{1},{2}",
                location.ha.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture),
                location.ha.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture),
                (double.IsNaN(location.va) ? 99.0d : location.va).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture));

            gga = "$" + gga + NmeaChecksum(gga);
            gll = "$" + gll + NmeaChecksum(gll);
            rmc = "$" + rmc + NmeaChecksum(rmc);
            gsa = "$" + gsa + NmeaChecksum(gsa);

            return gga + "\r\n"
                + gll + "\r\n"
                + rmc + "\r\n"
                + gsa + "\r\n";
        }

        private static string NmeaCoord(double coord, bool isLat)
        {
            string direct;
            string format;

            if (isLat)
            {
                if (coord < 0)
                    direct = "S";
                else
                    direct = "N";
                format = "{0:D2}{1},{2}";
            }
            else
            {
                if (coord < 0)
                    direct = "W";
                else
                    direct = "E";
                format = "{0:D3}{1},{2}";
            }

            coord = Math.Abs(coord);
            var degs = (int)coord;
            var mins = (coord - degs) * 60.0;

            return string.Format(format, degs, mins.ToString("00.0000", System.Globalization.CultureInfo.InvariantCulture), direct);
        }

        private static string NmeaChecksum(string sentence)
        {
            var checksum = 0;

            for (var i = 0; i < sentence.Length; i++)
                checksum ^= sentence[i];

            return "*" + checksum.ToString("X2");

        }
    }

    class Location
    {
        public double lat { get; set; }
        public double lon { get; set; }
        public double alt { get; set; }
        public double speed { get; set; }
        public double ha { get; set; }
        public double va { get; set; }
        public DateTimeOffset time { get; set; }
        public DateTime localTime { get; set; }

        public Location()
        {
        }

        public Location(Location original)
        {
            lat = original.lat;
            lon = original.lon;
            alt = original.alt;
            speed = original.speed;
            ha = original.ha;
            va = original.va;
            time = original.time;
        }
    }
}
