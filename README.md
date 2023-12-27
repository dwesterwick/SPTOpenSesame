A tool for developers (and cheaters) to open locked doors, safes, etc. without requiring the key.

This mod will add a new action called "Open Sesame" to their context menus to allow you to open them without requiring the key.

It also adds a "Turn On Power" action to doors that require the map's power switch to be turned on before they can be unlocked.

To prevent you from accidentally unlocking things, a "Do Nothing" action is added first (so it's the default action). This can be disabled in the Configuration Manager.

You can enable options in the Configuration Manager to write debug messages when the context menu opens or when you select the "Open Sesame" or "Turn on Power" actions. Enabling these will allow you to see the door ID (when the context menu opens), key ID (when you unlock a door via the "Open Sesame" action), or switch ID (when you turn on the power switch).

You can also prevent this mod from adding actions to context menus via a Configuration Manager option.

I wrote this to be agnostic of the SPT/EFT version, so you should rarely (if ever) have to update it.

Translations for the new context-menu actions exist for:
* English
* Chinese
* French
* German
* Korean
* Portuguese
* Russian
* Spanish

If you would like to help me include other languages, please post a comment with the locale ID in *Aki_Data\Server\database\locales\global* and translations for:
* "Do Nothing"
* "Open Sesame"
* "Turn On Power"

Known Issues:
* Cannot directly open the saferoom door in Interchange. You can only open it via the keypad in the Burger Spot restroom. 