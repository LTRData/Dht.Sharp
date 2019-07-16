# Dht.Sharp, modified by Olof Lagerkvist, LTR Data

This is a modification to Daniel Perry's Dht11/Dht22 C# library for Windows IoT Core on Raspberry Pi. This modification requires only one single GPIO pin for each connected sensor. It uses P/Invoke calls to performance counter (high-accuracy timer) instead of GPIO ChangeReader.

Original project: https://github.com/porrey/Dht.Sharp
