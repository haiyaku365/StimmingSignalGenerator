# StimmingSignalGenerator
### Changelog:
#### v0.5.0.0
##### Changes
* Remove WaveOut audio API.
* Upgrade to .Net 8
* Various bug fix.
##### New
* Add PortAudio audio API.
---
#### v0.4.0.0
##### Changes
* On startup load last playlist instead of first playlist sort by file name.
* Tweak UI.
##### New
* UI Data persistence.
* ZCP Modulation.
* Include demo playlist.
* Switch track in timing mode (hotkey, context menu).
* Shuffle in timing mode.
* Copy, paste playlist on save, load button context menu.
* Fade in/out when play/stop.
* Add development and waveform chatroom menu in information button.
* Crossfade in timing mode.
* Crossfade when switch track.
* Add WaveOut Audio API.
---
#### v0.3.1.0
##### Changes
* Increase UI responsive when enable plot.
* Fixes negative phase calculation.
* Fixes stereo volume control slider not load properly.
##### New
* Add Cross-platform Audio API and now able to run on Linux.
* White and pink noise frequency can be control.
* Hide zero modulation mode.
* Quick save button.
---
#### v0.3.0.1
##### Changes
* Timing mode no longer reset to first track on edit timing or add, move, remove track.
* Control slider box allow edit max, min value while playing.
* Control slider box commit value when scroll mouse wheel, hit enter, lost focus and cancel when hit esc.
* Fixes signal initially jumpy in timing mode.
* Fixes freeze when playing high frequency.
* Improve UI.
##### New
* Signal can be sync frequency with another signal and control phase shift related to that signal.
* Add phase modulation.
* Add playlist note.
* Add hot key for play, stop and track playing.
* Add Latency configuration.
---
#### v0.2.1.0
##### Changes
* Fixes track name not load.  
* Fixes cannot right click on track list.  
* Fixes freeze when change track timing while playing.  
##### New
* Add Master volume.  
---
#### v0.2.0.0  
* Add playlist feature
---
#### v0.1.0.0  
First release