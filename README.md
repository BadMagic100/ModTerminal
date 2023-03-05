# ModTerminal

A Hollow Knight DebugMod addon which provides a text-based terminal for commands and cheats

## Usage

To use the mod, start by configuring a keybind in DebugMod. The "Toggle Terminal" bind appears under the "Misc" category.

After toggling the console open, you can type commands and press enter to submit. Commands can take any number of parameters.
Parameters are generally separated by whitespace, which is ignored in most circumstances, and can be provided in one of two ways:

* Ordered parameters, or positional parameters, are provided simply by entering a value in the same order it appears in the help
  documentation. Ordered parameters must always be provided at the beginning of the command, and cannot be provided after a named
  parameter has been provided.
* Named parameters can be provided in any order, using the syntax `name=value`, `--name=value`, or `--name value`. The help 
  documentation for each command includes the names of each parameter. This style of input is typically used to skip over optional
  parameters.

Some commands can take a variable number of parameters. This is accomplished by using an array as the last parameter. You can provide
any number of values, including zero, as ordered parameters in this slot. You may use either named or positional syntax to provide
values; if using named parameters, simply use the same parameter name multiple times.

In many situations, string values may be provided as normal. However, if your desired value includes whitespace or certain special
characters, you can use string literal syntax to craft your string value, `"like this"`. You can use the backslash character `\` to
escape newlines `\n`, quotes `\"`, and other backslashes `\\` within these strings.

This mod comes included with several built-in commands. Use the `listcommands` and `help` commands to view their documentation.

## Developers

Developers can add their own commands to the terminal. Note that if you don't need parameters, it's usually better to use a DebugMod keybind.
To add a command, simply call `CommandTable.RegisterCommand`. You may also use `CommandTable.RegisterGroup` to create groups of commands.
This mod uses its own API; there are several usage examples [here](https://github.com/BadMagic100/ModTerminal/blob/master/ModTerminal/ModTerminal.cs).

You can add documentation to your command by using the `[HelpDocumentation]` attribute. This attribute can be applied to both methods and parameters.
Documentation about the name and type of parameters is inferred automatically; any provided documentation serves only as additional description.

You can convert non-standard parameters using the `[ParameterConverter]` attribute. This attribute is applied to parameters to control how values
are converted.

Command methods can take a Command instance as their first parameter to access metadata about themselves and their current execution.
This is particularly pertinent for long-running commands, which may use `AsyncCommand` in tandem with the command's execution context
to report progress and completion without blocking the UI thread and freezing the game.