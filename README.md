# ModTerminal

A Hollow Knight DebugMod addon which provides a text-based terminal for commands and cheats

## Usage

To use the mod, start by configuring a keybind in DebugMod. The "Toggle Terminal" bind appears under the "Misc" category.

After toggling the console open, you can type commands and press enter to submit. Commands can take any number of parameters.
Parameters are separated by spaces, and can be provided in one of two ways:

* Ordered parameters, or positional parameters, are provided simply by entering a value in the same order it appears in the help
  documentation. Ordered parameters must always be provided at the beginning of the command, and cannot be provided after a named
  parameter has been provided.
* Named parameters can be provided in any order, using the syntax `name=value`. Note the lack of spaces. The help documentation for
  each command includes the names of each parameter. This style of input is typically used to skip over optional parameters.

Some commands can take a variable number of parameters. This is accomplished by using an array as the last parameter. You can provide
any number of values, including zero, as ordered parameters in this slot. Additional values will also be included. Note that because
array parameters are positional, you cannot use named parameters for these commands.

This mod comes included with several built-in commands. Use the `listcommands` and `help` commands to view their documentation.

## Developers

Developers can add their own commands to the terminal. Note that if you don't need parameters, it's usually better to use a DebugMod keybind.
To add a command, simply call `CommandTable.RegisterCommand`. This mod uses its own API; there are several usage examples [here](https://github.com/BadMagic100/ModTerminal/blob/4b7dcbacc9553b3b5549f66e1fcbe0bdbbb02f57/ModTerminal/ModTerminal.cs#L50-L60).

You can add documentation to your command by using the `[HelpDocumentation]` attribute. This attribute can be applied to both methods and parameters.
Documentation about the name and type of parameters is inferred automatically; any provided documentation serves only as additional description.