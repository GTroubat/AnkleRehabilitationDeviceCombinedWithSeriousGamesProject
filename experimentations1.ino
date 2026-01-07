# include <Dynamixel.h>
# include <HX711.h>



// Definition of the HX711 sensors
HX711 scale1;
HX711 scale2;
HX711 scale3;
HX711 scale4;

// Convert to international convention
String floatToString(float value) {
  char temp[20]; //Initialise a char table (= string) named temp of a good size to be sure to include the float
  dtostrf(value, 10, 4, temp); //Convert float into string (table of char) in the buffer with 4 decimal
  String result = String(temp);
  result.replace(".", ","); // Replace , by .
  return result;
}

void setup() {
  Serial.begin(9600);
  // Add pin to sensors
  scale1.begin(2, 3);
  scale2.begin(4, 5);
  scale3.begin(6, 7);
  scale4.begin(8, 9);
}

void loop() {
  // Read sensor
  float value1 = scale1.get_units();
  float value2 = scale2.get_units();
  float value3 = scale3.get_units();
  float value4 = scale4.get_units();
  if (value1 < 0) value1 = 0;
  if (value2 < 0) value2 = 0;
  if (value3 < 0) value3 = 0;
  if (value4 < 0) value4 = 0;

  // Send to serial port with ; between values and by converting float into international convention
  Serial.print(floatToString(value1));
  Serial.print(";");
  Serial.print(floatToString(value2));
  Serial.print(";");
  Serial.print(floatToString(value3));
  Serial.print(";");
  Serial.println(floatToString(value4));

  delay(10); // Wait 10ms to avoid too much values and because unity read every 0.01s
}

