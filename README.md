# Snippeter, a c# snippet extension for Visual Studio 2019

[![Build status](https://ci.appveyor.com/api/projects/status/s5lpx2ignav8idxo?svg=true)](https://ci.appveyor.com/project/LaraSQP/snippeter)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](license.txt)


This extension makes it possible to add/edit/remove `c# snippets` directly from the IDE, instead of manually crafting XML snippet templates with the appropriate data to then import them via the built-in **Code Snippets Manager**.


## Setup

Install from the `Open VSIX gallery` via the `Tools -> Extensions and Updates` menu or [download](http://vsixgallery.com/extensions/Snippeter.205e93a2-67fd-418d-a773-558dbce0ffd0/Snippeter%20v1.0.16.vsix)  as a `VSIX` package and install it manually.

## Getting Started

- Once installed, the `Tools` menu will show the entry ![Command](https://user-images.githubusercontent.com/12540983/69513781-57237100-0f8c-11ea-922d-eba6925ddc74.png) `Add snippet`, as shown below:

![tools](https://user-images.githubusercontent.com/12540983/69891035-d920ea80-133c-11ea-8f7d-c902cdbbfcc5.png)

- A quick way to get to **Snippeter** is to add the extension to the toolbar, as shown in the image below (bottom right below) or to manually assign a shortcut to it via `Options -> Keyboard -> Tools.AddSnippet`.

![toolbar](https://user-images.githubusercontent.com/12540983/69891033-d920ea80-133c-11ea-8dbc-6ec19e0ddcf5.png)

- If code has been selected in the editor when **Snippeter** is run, the window below pops up. There, it is possible to modify the code and add a description, shortcut, and title. The last two in red are required (as is some code).

- **Snippeter** uses the `Avalon editor` so it has some of its nice features available, such as syntax-highlighting.

- After adding a snippet, the status bar will notify you of the achievement for a few seconds.

![editor](https://user-images.githubusercontent.com/12540983/69891031-d8885400-133c-11ea-9dc2-0fcafc88f26b.png)

- If **no** code has been selected in the editor when **Snippeter** is run, it works in manager mode (see below) listing all snippets in your `c#` **My Code Snippets** folder.

![manager](https://user-images.githubusercontent.com/12540983/69891032-d8885400-133c-11ea-8fec-f9e0ee2aa6ca.png)

- Press *delete* to remove the selected snippet from the **Code Snippets Manager** (non-reversible operation).

- Right-click on a listed snippet to code its full path to the clipboard.

- Hover over an item in the list of snippets to see its description, code, and location without having to open it.

- Double-click on a snippet to open it and modify its code in **Snippeter's** `Avalon editor` or to change its shortcut, description, or title, as shown below.

![editing](https://user-images.githubusercontent.com/12540983/69891396-a2989f00-133f-11ea-8837-6bdebdca18fd.png)

- Click `Update snippet` to save any changes to the snippet or `Dismiss` to disregard any modification made. Both operations return to manager mode.

- Click on `Open snippet file` to open the snippet file in the `IDE`. Be warned that other extensions might interfere with this operation (see [Conflicts](#Conflicts) section below) and hang Visual Studio.

## Note

- If you want to work with your snippets, open **Snippeter** without selecting code. When code is selected, **Snippeter** is set to add that selected code as a new snippet and nothing more.

## Limitations

**Snippeter** only works with `c# snippets`. If you need something else, fork the code. It's a mess but it works, I think.

## Conflicts

The extension [Snippet Designer](https://marketplace.visualstudio.com/items?itemName=vs-publisher-2795.SnippetDesigner) interferes with, at least, attempts to open snippets in the `IDE` and, thus, limits the functionality of **Snippeter** if both are installed simultaneously. If `Snippet Designer` is indeed installed and does not hang Visual Studio, the snippet file will not be displayed in XML but via `Snippet Designer` (for better or worse).
