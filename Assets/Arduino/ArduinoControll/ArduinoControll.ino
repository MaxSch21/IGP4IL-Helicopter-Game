#include <Servo.h>

const int joystickXPin = A0;
const int potentiometerPin = A2;
const int fuelServoPin = 9;
const int baudRate = 9600;
const int sendDelayMs = 20;

const int displaySegmentA = 2; // display pin 7
const int displaySegmentB = 3; // display pin 6
const int displaySegmentC = 4; // display pin 4
const int displaySegmentD = 5; // display pin 2
const int displaySegmentE = 6; // display pin 1
const int displaySegmentF = 7; // display pin 9
const int displaySegmentG = 8; // display pin 10

Servo fuelServo;
String serialCommand = "";

void setup()
{
    Serial.begin(baudRate);
    fuelServo.attach(fuelServoPin);
    fuelServo.write(0);

    pinMode(displaySegmentA, OUTPUT);
    pinMode(displaySegmentB, OUTPUT);
    pinMode(displaySegmentC, OUTPUT);
    pinMode(displaySegmentD, OUTPUT);
    pinMode(displaySegmentE, OUTPUT);
    pinMode(displaySegmentF, OUTPUT);
    pinMode(displaySegmentG, OUTPUT);
    showPackageCount(0);
}

void loop()
{
    int joystickX = analogRead(joystickXPin);
    int potentiometerRaw = analogRead(potentiometerPin);
    int joystickMapped = map(joystickX, 0, 1023, -100, 100);
    int potentiometerMapped = map(potentiometerRaw, 0, 1023, -100, 100);

    Serial.print(joystickMapped);
    Serial.print(",");
    Serial.println(potentiometerMapped);

    readFuelServoOutput();
    delay(sendDelayMs);
}

void readFuelServoOutput()
{
    while (Serial.available() > 0)
    {
        char character = Serial.read();

        if (character == '\n')
        {
            handleSerialCommand(serialCommand);
            serialCommand = "";
            continue;
        }

        if (character != '\r')
            serialCommand += character;
    }
}

void handleSerialCommand(String command)
{
    command.trim();

    if (command.startsWith("P:"))
    {
        showPackageCount(command.substring(2).toInt());
        return;
    }

    if (!command.startsWith("F:"))
        return;

    int angle = command.substring(2).toInt();
    int constrainedAngle = constrain(angle, 0, 180);
    fuelServo.write(constrainedAngle);
    Serial.print("S:");
    Serial.println(constrainedAngle);
}

void showPackageCount(int packagesLeft)
{
    int number = constrain(packagesLeft, 0, 9);

    switch (number)
    {
        case 0: showSegments(1, 1, 1, 1, 1, 1, 0); break;
        case 1: showSegments(0, 1, 1, 0, 0, 0, 0); break;
        case 2: showSegments(1, 1, 0, 1, 1, 0, 1); break;
        case 3: showSegments(1, 1, 1, 1, 0, 0, 1); break;
        case 4: showSegments(0, 1, 1, 0, 0, 1, 1); break;
        case 5: showSegments(1, 0, 1, 1, 0, 1, 1); break;
        case 6: showSegments(1, 0, 1, 1, 1, 1, 1); break;
        case 7: showSegments(1, 1, 1, 0, 0, 0, 0); break;
        case 8: showSegments(1, 1, 1, 1, 1, 1, 1); break;
        case 9: showSegments(1, 1, 1, 1, 0, 1, 1); break;
    }
}

void showSegments(int a, int b, int c, int d, int e, int f, int g)
{
    digitalWrite(displaySegmentA, a);
    digitalWrite(displaySegmentB, b);
    digitalWrite(displaySegmentC, c);
    digitalWrite(displaySegmentD, d);
    digitalWrite(displaySegmentE, e);
    digitalWrite(displaySegmentF, f);
    digitalWrite(displaySegmentG, g);
}
