using System;

namespace Quill.Input.Definitions;

[FlagsAttribute]
public enum PortA : byte
{
  Joy1Up    = 0b_0000_0001,
  Joy1Down  = 0b_0000_0010,
  Joy1Left  = 0b_0000_0100,
  Joy1Right = 0b_0000_1000,
  Joy1FireA = 0b_0001_0000,
  Joy1FireB = 0b_0010_0000,
  Joy2Up    = 0b_0100_0000,
  Joy2Down  = 0b_1000_0000,
  All       = 0b_1111_1111
}