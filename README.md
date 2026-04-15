# Dht.Sharp, modified by Olof Lagerkvist, LTR Data

This is a modification to Daniel Perry's Dht11/Dht22 C# library for Windows IoT Core on Raspberry Pi. This modification requires only one single GPIO pin for each connected sensor. It uses performance counter (high-accuracy timer) instead of GPIO ChangeReader. Works on both Windows and Linux.

Original project: https://github.com/porrey/Dht.Sharp
This fork: https://github.com/LTRData/Dht.Sharp
