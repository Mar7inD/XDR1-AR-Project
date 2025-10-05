# Title

[Back](../README.md)

_Written by: Martin Donchev_

The scaling, movement and rotation challenge
Final polishing
Menu hide/show interactivity plus plane hide/show


Have to fix on top script to adjust always on top 

iOS audio not playing. Found out after several hours research that I have to toggle the mute switch in order to have audio.

The touch gestures where very hard to implement. Got several ideas but the finger count was the best success and most robust so I went with it. 

Firstly I did not knew that there are always 10 inputs like the fingers. So I went like with a keyboard and mouse. If we have 1 input or 2 ...
Then after long time debugging I figured out that I have 10 inputs. As I call them ghost inputs. I understood about interesting data like pressure and phase where each input carries. Sometimes there is also radius data. I did not had that. But with checking for phase and pressure I easily distinguished the active inputs. I also had issue with the finger sensitivity because you can not put or remove your 3 fingers at the same time I implemented debounce and also implemented 4 finger system. 