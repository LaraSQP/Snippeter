# Snippeter, a c# snippet extension for Visual Studio 2019

There is nothing to this extension. It simply makes it easier to add/edit/remove snippets from the built-in **Code Snippets Manager** directly from the IDE.

## Setup

Once installed, a square blue icon ![Command](https://user-images.githubusercontent.com/12540983/69513781-57237100-0f8c-11ea-922d-eba6925ddc74.png) with the text *Add snippet* will appear in the **Tools** menu as shown below:

![tools](https://user-images.githubusercontent.com/12540983/69513786-57bc0780-0f8c-11ea-85c9-5a57354c0565.png)

A quick way to get to **Snippeter** is to add the extension to the toolbar (botton right below).

![toolbar](https://user-images.githubusercontent.com/12540983/69513785-57bc0780-0f8c-11ea-8eb6-46229e087459.png)

If code has been selected in an editor when **Snippeter** is run, the window below pops up. There, it is possible to modify the code and add a description, shortcut, and title. The last two in red are required (as is the code).

**Snippeter** uses the **Avalon editor** so it has some of its nice features available.

After adding a snippet, the status bar will notify you of the achievement for a few seconds.

![add new](https://user-images.githubusercontent.com/12540983/69513780-57237100-0f8c-11ea-99c1-243414ec629d.png)

If **no** code has been selected in an editor when **Snippeter** is run, it pops up in manager mode (see below).

![manager](https://user-images.githubusercontent.com/12540983/69513784-57237100-0f8c-11ea-942a-047eba301f67.png)

All snippets in your **My Code Snippets** folder will be listed.

Hover over the list of snippets to see the code and location of each.

Press *delete* to remove the selected snippet from the **Code Snippets Manager** (non-reversible operation).

Double-click on a snippet to open it and modify it in **Snippeter's Avalon editor** or to change its shortcut, description, or title, as shown earlier.

If you want to work with your snippets, open **Snippeter** without selecting code. When code is selected, **Snippeter** is set to add that selected code as a new snippet and nothing more.

## Limitations

**Snippeter** only works with c# snippets. If you need something else, fork the code. It's a mess but it works, I think.
