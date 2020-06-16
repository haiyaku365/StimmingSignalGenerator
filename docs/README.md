# StimmingSignalGenerator
This software basically generate real-time audio signal.  
[![Gitter](https://badges.gitter.im/StimmingSignalGenerator/Development.svg)](https://gitter.im/StimmingSignalGenerator/Development?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge)

### screenshot
![Window1](v0.3.1.0_1.png)

![Window2](v0.3.1.0_2.png)

## Running StimmingSignalGenerator
#### To run on Windows
1. Install [.NET Core 3.1 Desktop Runtime](https://dot.net).
2. Download binary in [releases page](https://github.com/haiyaku365/StimmingSignalGenerator/releases)
3. run exe

#### To run on Linux
1. Install [.NET Core 3.1 runtime](https://dot.net)
2. Install OpenAL lib
```
sudo apt install libopenal1
```
3. Download binary in [releases page](https://github.com/haiyaku365/StimmingSignalGenerator/releases)
4. run
```
dotnet PathToApp/StimmingSignalGenerator.dll
```
#### To run from source
1. Install [.NET Core 3.1 SDK](https://dot.net)
2. clone
```
git clone https://github.com/haiyaku365/StimmingSignalGenerator.git
```
3. run
```
cd StimmingSignalGenerator/StimmingSignalGenerator
dotnet run
```

## Using StimmingSignalGenerator
#### Basic signal 
Basic signal can control frequency, gain, 
zero crossing position and add FM, AM, PM and ZM.

#### Zero Crossing Position(ZCP)
zero crossing position(ZCP) is for control signal positive, negative period.  
Usually combine with AM square wave to control on off period.  
ZCP 0.2 mean on 20% and off 80%.

#### Sync
When sync with another signal, 
frequency will be in sync and phase shift control will appear.

#### Control slider
Control slider can adjust max-min value for precise control.  
Input value in box commit when hit enter or lost focus and cancel when hit esc.  
When focus input box using mouse scroll will change it value.

#### Right click to copy, paste
Playlist, Track, Signal and Control slider can be copy to clipboard.  
Copy value is basically text(json) and can paste across playlist.  
**Playlist:** right click on save, load button. (playlist copy as json compressed base64 text)  
**Track:** right click on list to copy right click on add button to paste.  
**Signal:** right click on header to copy right click on add button to paste.  
**Control slider:** right click on control slider.  

#### Mono, Stereo
Mono mode use one signal for both L,R but can control volume of each channel.  
Stereo mode have different signal on each channel.

#### Track
Track order can be change by drag and drop.  
To edit track name select track in playlist and click track name on the right side(track header) to enter edit mode.

#### Plot
Enable plot to plot output signal.  
Enable HD Plot will plot in High definition in cost of more cpu.  
Use mouse scroll to zoom in, out.

#### Hot key
*Toggle Play, Stop:* Ctrl+~  
*Play track:* Ctrl+1, Ctrl+2, ... , Ctrl+0

#### Note
Note that save along with playlist.

#### ~~Default playlist~~
~~When startup it will load first playlist file sort by file name.~~  
No default playlist anymore.  
When startup it will load playlist you open before exit.

