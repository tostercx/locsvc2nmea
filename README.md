## About

A background service that reads windows location API, converts it to NMEA 0183
and sends it ot a virtual COM port.

Currently ports are hardcoded and output goes to COM48. COM49 is used by the
service to write data.


## Credits

### NMEA

NMEA generator modified from https://github.com/EarToEarOak/Location-Bridge

### Virtual COM port

Signed com0com drivers from https://pete.akeo.ie/2011/07/com0com-signed-drivers.html
