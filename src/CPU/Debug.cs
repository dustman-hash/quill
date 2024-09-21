using Quill.Common;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Quill.CPU;

// Implementation of SDSC Debug Console Specification from SMS Power
// https://www.smspower.org/Development/SDSCDebugConsoleSpecification
unsafe public ref partial struct Z80
{
  #if DEBUG
  private char[][] _sdsc = new char[25][];
  private int _row = 0;
  private int _col = 0;
  private char[] _dataFormats = { 'd', 'u', 'x', 'X', 'b', 'a', 's' };

  // control port state 
  private bool _expectAttribute = false;
  private bool _expectRow = false;
  private bool _expectCol = false;

  // data port state
  private bool _expectWidth = false;
  private bool _expectFormat = false;
  private bool _expectType0 = false;
  private bool _expectType1 = false;
  private bool _expectByte = false;
  private bool _expectWord0 = false;
  private bool _expectWord1 = false;
  private bool _autoWidth = false;
  private byte _dataWidth = 0;
  private char _dataFormat = ' ';
  private string _dataType = string.Empty;
  private ushort _parameter = 0;
  
  public void InitializeSDSC()
  {
    _row = 0;
    _col = 0;
    _sdsc = new char[25][];

    for (int row = 0; row < 25; row++)
    {
      _sdsc[row] = new char[80];
      for (int col = 0; col < 80; col++)
        _sdsc[row][col] = ' ';
    }
    
    _expectAttribute = false;
    _expectRow = false;
    _expectCol = false;
    _expectWidth = false;
    _expectFormat = false;
    _expectType0 = false;
    _expectType1 = false;
    _expectByte = false;
    _expectWord0 = false;
    _expectWord1 = false;
  }

  private void PrintSDSC()
  {
    var output = string.Empty;
    for (int col = 0; col < 80; col++)
      output += _sdsc[_row-1][col].ToString();
    Debug.WriteLine(output);
  }

  private void ScrollSDSC()
  {
    for (int i = 0; i < 24; i++)
      Array.Copy(_sdsc[i+1], _sdsc[i], 25);
    _sdsc[24] = new char[80];
  }

  private void ControlSDSC(byte value)
  {
    if (_expectAttribute)
      _expectAttribute = false;
    else if (_expectRow)
    {
      _row = value % 25;
      _expectRow = false;
      _expectCol = true;
    }
    else if (_expectCol)
    {
      _col = value % 80;
      _expectCol = false;
      PrintSDSC();
    }
    else if (value == 0x01)
      throw new Exception("Emulation suspended.");
    else if (value == 0x02)
      InitializeSDSC();
    else if (value == 0x03)
      _expectAttribute = true;
    else if (value == 4)
      _expectRow = true;
    else
      Debug.WriteLine($"[ERROR] Invalid command: {value.ToHex()}.");
  }

  private void WriteSDSC(byte value)
  {
    var ascii = Encoding.ASCII.GetString(new byte[] {value})[0];
    if (_expectWidth)
    {
      _expectWidth = false;
      if (value == 0)
      {
        Debug.WriteLine($"[ERROR] Data cannot have a width of 0.");
        return;
      }
      else if (value == 37)
      {
        _sdsc[_row][_col] = ascii;
        return;
      }
      
      _expectFormat = true;
      if (value >= 48 && value <= 57)
      {
        _autoWidth = false;
        _dataWidth = value;
      }
      else
      {
        _autoWidth = true;
        WriteSDSC(value);
      }
    }
    else if (_expectFormat)
    {
      _expectFormat = false;
      _dataFormat = ascii;
      if (_dataFormats.Contains(_dataFormat))
        _expectType0 = true;
      else
        Debug.WriteLine("[ERROR] Invalid data format: " + _dataFormat);
    }
    else if (_expectType0)
    {
      _expectType0 = false;
      _expectType1 = true;
      _dataType = ascii.ToString();
    }
    else if (_expectType1)
    {
      _expectType1 = false;
      _dataType += ascii;

      if (!_dataType.EndsWith('b') && 
          (_dataFormat == 'a' || _dataFormat == 's'))
      {
        Debug.WriteLine($"[ERROR] Invalid data format/type combination: {_dataFormat}/{_dataType}");
        return;
      }
      
      switch(_dataType)
      {
        case "mw":
        case "mb":
        case "vw":
        case "vb":
          _expectWord0 = true;
          return;

        case "pr":
        case "vr":
          _expectByte = true;
          return;

        default:
          Debug.WriteLine("[ERROR] Invalid data type: " + _dataType);
          return;
      }
    }
    else if (_expectByte)
    {
      _expectByte = false;
      _parameter = value;
      PrintData();
    }
    else if (_expectWord0)
    {
      _expectWord0 = false;
      _expectWord1 = true;
      _parameter = value;
    }
    else if (_expectWord1)
    {
      _expectWord1 = false;
      _parameter += (ushort)(value << 8);
      PrintData();
    }
    else if (value == 10)
    {
      _col = 0;
      if (_row == 24)
        ScrollSDSC();
      else
        _row++;
        
      PrintSDSC();
    }
    else if (value == 13)
    {
      _col = 0;
      PrintSDSC();
    }
    else if (value == 37)
    {
      _expectWidth = true;
    }
    else if (value < 32 || value > 127)
    {
      Debug.WriteLine($"[ERROR] Undefined character: {value.ToHex()} at instruction {_instruction}");
    }
    else 
    {
      WriteCharacter(value);
    }
  }

  private char ByteToChar(byte value) => Encoding.ASCII.GetString(new byte[]{value})[0];
  private void WriteCharacter(byte value) => WriteCharacter(ByteToChar(value));
  private void WriteCharacter(char value)
  {
    _sdsc[_row][_col] = value;
    _col++;

    if (_col == 80) 
    {
      _col = 0;
      if (_row == 24)
        ScrollSDSC();
      else
        _row++;
        
      PrintSDSC();
    }
  }
  
  private void PrintData()
  {
    var formatted = _dataType switch
    {
      "mb" => FormatMemoryByte(),
      "mw" => FormatMemoryWord(),
      "pr" => FormatRegister(),
      "vb" => FormatVramByte(),
      "vw" => FormatVramword(),
      "vr" => PrintVDPRegister()
    };

    if (_autoWidth)
      _dataWidth = (byte)formatted.Length;

    var padded = string.Empty;
    if (_dataWidth < formatted.Length)
      padded = formatted.Substring(0, _dataWidth);
    else
    {
      var padding = _dataWidth - formatted.Length;
      for (int p = 0; p < padding; p++)
        padded = ' ' + padded;
    }

    for (int i = 0; i < _dataWidth; i++)
      WriteCharacter(padded[i]);
  }

  private string FormatMemoryByte()
  {
    var value = _memory.ReadByte(_parameter++);
    var display = string.Empty;
    var bytes = new byte[]{value};

    switch (_dataFormat)
    {
      case 'd': return unchecked((int)value).ToString();
      case 'u': return ((int)value).ToString();
      case 'x': 
      case 'X': return value.ToHex();
      case 'b': return Convert.ToString(value, 2);
      case 'a': 
        if (_autoWidth) 
          _dataWidth = 1;
        for (int i = 1; i < _dataWidth; i++)
          bytes[i] = _memory.ReadByte(_parameter++);
        return Encoding.ASCII.GetString(bytes);

      case 's':
        if (_autoWidth) 
          _dataWidth = byte.MaxValue;
        for (int i = 1; i < _dataWidth; i++)
        {
          value = _memory.ReadByte(_parameter++);
          if (value == 0)
            break;
          bytes[i] = value;
        }
        return Encoding.ASCII.GetString(bytes);
          
      default:
        return value.ToHex();
    }
  }

  private string FormatMemoryWord()
  {
    var value = _memory.ReadWord(_parameter);
    var display = string.Empty;

    switch (_dataFormat)
    {
      case 'd': return unchecked((int)value).ToString();
      case 'u': return ((int)value).ToString();
      case 'x': 
      case 'X': return value.ToHex();
      case 'b': return Convert.ToString(value, 2);
      default:
        return value.ToHex();
    }
  }

  private string FormatRegister()
  {
    // TODO: formatting
    return _parameter switch
    {
      0x00 or 0x62 => _b.ToHex(),
      0x01 or 0x63 => _c.ToHex(),
      0x02 or 0x64 => _d.ToHex(),
      0x03 or 0x65 => _e.ToHex(),
      0x04 or 0x68 => _h.ToHex(),
      0x05 or 0x6C => _l.ToHex(),
      0x06 or 0x66 => ((byte)_flags).ToHex(),
      0x07 or 0x61 => _a.ToHex(),
      0x08 or 0x70 => _pc.ToHex(),
      0x09 or 0x73 => _sp.ToHex(),
      0x0A or 0x78 => _ix.ToHex(),
      0x0B or 0x79 => _iy.ToHex(),
      0x0C or 0x42 => _bc.ToHex(),
      0x0D or 0x44 => _de.ToHex(),
      0x0E or 0x48 => _hl.ToHex(),
      0x0F or 0x63 => _af.ToHex(),
      0x10 or 0x72 => _r.ToHex(),
      0x11 or 0x69 => _r.ToHex(),
      0x12 => _bcShadow.ToHex(),
      0x13 => _deShadow.ToHex(),
      0x14 => _hlShadow.ToHex(),
      0x15 => _afShadow.ToHex(),
    };
  }

  private string FormatVramByte()
  {
    throw new Exception("VDP not implemented");
  }

  private string FormatVramword()
  {
    throw new Exception("VDP not implemented");
  }

  private string PrintVDPRegister()
  {
    throw new Exception("VDP not implemented");
  }
  #endif
}