<img width="520" height="515" alt="image" src="https://github.com/user-attachments/assets/a9ebec41-09a2-4341-9de2-8527d62dcd6e" />


Well here's my first attempt at making a C# Rainmeter Plugin, so don't be harsh :)
GitHub link for feedback and updates:

Try it out live in this Skin: https://www.deviantart.com/fediafedia/art/Big-Sur-26-for-Rainmeter-846882462

HOW IT WORKS:

Basically it grabs the wallpaper from the registry, resizes it a lot, and dishes out chunks of that wallpaper for Rainmeter skins that are using the Plugin.
Since it passes the X, Y, W and H of the Widget it creates a realisitc Blur effect under it.

It's not live blur, like Windows Aero or Liquid Glass (with its millions of lines of shader code), but it's a very sleek and fast solution that doesn't increase the resource use on your computer.

But two downsides:
1. You must refresh all skins using the plugin if you changed the wallpaper (it will not auto-detect a change)
2. If you moved a skin, it will not re-apply the blur until you refresh it, or send a !ReloadBlur bang.

We are working to resolve these two and maybe someone in the Rainmeter community can help us with that :)

HOW TO USE IT IN YOUR SKINS:

First, include EasyBlur.dll with the Plugins folder.
Then include these lines in your Skin(s):

[EasyBlur]
Measure=Plugin
Plugin=EasyBlur
ScreenAreaWidth=#ScreenAreaWidth#
ScreenAreaHeight=#ScreenAreaHeight#
XPos=#CURRENTCONFIGX#
YPos=#CURRENTCONFIGY#
XWidth=520
XHeight=328

[Blur]
Meter=Image
ImageName=[EasyBlur]
DynamicVariables=1
solidcolor=0,0,0,255
w=500
h=300
x=10
y=10

The [Blur] will be the actual image, so you can do stuff to it also, such as applying effects, re-sizing it, or adding more meters to layer it. See the example of this Skin, by right-clicking and selecting Edit.

ADDITIONAL OPTIONS
The demo has these 3 options that you can play around with.

This lets you change how intense the blur should be:
Intensity=3

Creates a magnifying-glass like effect, depending on value:
Zoom=1.5

Will use a provided image instead of the windows wallpaper as the blur:
ImagePath=C:\Users\User\Pictures\Wall.jpg

EXTRA REFRESH

If you include this line under [Rainmeter] in your skin, it will, at least, refresh the blur when you moved the widget (and moved your mouse away from it)

[Rainmeter]
Update=-1
MouseLeaveAction=!execute [!CommandMeasure "EasyBlur" "!ReloadBlur"]
