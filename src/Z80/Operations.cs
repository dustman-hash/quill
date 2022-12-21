using Quill.Definitions;
using Quill.Extensions;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Quill;

unsafe public ref partial struct CPU
{
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void ADC8()
  {
    var augend = _a;
    var addend = ReadByteOperand(_instruction.Source);
    if (_carry) addend++;
    var sum = augend + addend;
    
    var flags = (Flags)(sum & 0b_1010_1000);
    if (sum == 0)
      flags |= Flags.Zero;
    if ((augend & 0x0F) + (addend & 0x0F) > 0x0F)
      flags |= Flags.Halfcarry;
    if ((augend < 0x80 && addend < 0x80 && _sign) ||
        (augend >= 0x80 && addend >= 0x80 && !_sign))
      flags |= Flags.Parity;
    if (sum > byte.MaxValue)
      flags |= Flags.Carry;

    _a = (byte)sum;
    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void ADC16()
  {
    var augend = _hl;
    var addend = ReadWordOperand(_instruction.Source);
    if (_carry) addend++;
    var sum = augend + addend;

    var flags = (Flags)((sum >> 8) & 0b_1010_1000);
    if (sum == 0)
      flags |= Flags.Zero;
    if ((augend & 0x0FFF) + (addend & 0x0FFF) > 0x0FFF)
      flags |= Flags.Halfcarry;
    if ((augend < 0x8000 && addend < 0x8000 && _sign) ||
        (augend >= 0x8000 && addend >= 0x8000 && !_sign))
      flags |= Flags.Parity;
    if (sum > ushort.MaxValue)
      flags |= Flags.Carry;

    _hl = (ushort)sum;
    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void ADD8()
  {
    var augend = _a;
    var addend = ReadByteOperand(_instruction.Source);
    var sum = augend + addend;

    var flags = (Flags)(sum & 0b_1010_1000);
    if (sum == 0)
      flags |= Flags.Zero;
    if ((augend & 0x0F) + (addend & 0x0F) > 0x0F)
      flags |= Flags.Halfcarry;
    if ((augend < 0x80 && addend < 0x80 && _sign) ||
        (augend >= 0x80 && addend >= 0x80 && !_sign))
      flags |= Flags.Parity;
    if (sum > byte.MaxValue)
      flags |= Flags.Carry;

    _a = (byte)sum;
    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void ADD16()
  {
    var augend = ReadWordOperand(_instruction.Destination);
    var addend = ReadWordOperand(_instruction.Source);
    var sum = augend + addend;

    var flags = (Flags)((sum >> 8) & 0b_1010_1000);
    if (sum == 0)
      flags |= Flags.Zero;
    if ((augend & 0x0FFF) + (addend & 0x0FFF) > 0x0FFF)
      flags |= Flags.Halfcarry;
    if ((augend < 0x8000 && addend < 0x8000 && _sign) ||
        (augend >= 0x8000 && addend >= 0x8000 && !_sign))
      flags |= Flags.Parity;
    if (sum > ushort.MaxValue)
      flags |= Flags.Carry;

    WriteWordResult((ushort)sum);
    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void AND()
  {
    var result = _a & ReadByteOperand(_instruction.Source);

    var flags = (Flags)(result & 0b_1010_1000) | Flags.Halfcarry;
    if (result == 0)
      flags |= Flags.Zero;
    if (BitOperations.PopCount((byte)result) % 2 == 0)
      flags |= Flags.Parity;

    _a = (byte)result;
    _flags = flags;
  }
  
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void BIT()
  {
    var value = ReadByteOperand(_instruction.Source);
    var index = (byte)_instruction.Destination;
    var flags = (Flags)(value & 0b_1010_1000);
    
    // TODO: Handle undocumented flags for HLi and indexed cases
    if (!value.TestBit(index))
      flags |= Flags.Zero;

    flags |= Flags.Halfcarry;

    if (BitOperations.PopCount((byte)value) % 2 == 0)
      flags |= Flags.Parity;

    if (_carry)
      flags |= Flags.Carry;

    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void CALL()
  {
    var address = FetchWord();
    if (!EvaluateCondition())
      return;

    _sp -= 2;
    _memory.WriteWord(_sp, _pc);
    _pc = address;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void CCF()
  {
    var flags = (Flags)((byte)_flags & 0b_1100_0100);
    if (_carry)
      flags |= Flags.Halfcarry;
    else
      flags |= Flags.Carry;
    
    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void CP()
  {
    var subtrahend = ReadByteOperand(_instruction.Source);
    var difference = _a - subtrahend;

    var flags = (Flags)(difference & 0b_1010_1000) | Flags.Negative;
    if (difference == 0)
      flags |= Flags.Zero;
    if ((_a & 0x0F) - (subtrahend & 0x0F) < 0)
      flags |= Flags.Halfcarry;
    if ((_a < 0x80 && subtrahend >= 0x80 && _sign) ||
        (_a >= 0x80 && subtrahend < 0x80 && !_sign))
      flags |= Flags.Parity;
    if (subtrahend > _a)
      flags |= Flags.Carry;

    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void CPD()
  {
    var subtrahend = _memory.ReadByte(_hl);
    var difference = _a - subtrahend;

    _hl++;
    _bc--;
          
    var flags = (Flags)(difference & 0b_1010_1000) | Flags.Negative;
    if (difference == 0)
      flags |= Flags.Zero;
    if ((_a & 0x0F) - (subtrahend & 0x0F) < 0)
      flags |= Flags.Halfcarry;
    if (_bc != 0)
      flags |= Flags.Parity;
    if (_carry)
      flags |= Flags.Carry;

    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void CPDR()
  {
    CPD();
    if (_parity && !_zero)
      _pc -= 2;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void CPI()
  {
    var subtrahend = _memory.ReadByte(_hl);
    var difference = _a - subtrahend;

    _hl--;
    _bc--;
    
    var flags = (Flags)(difference & 0b_1010_1000);
    if (difference == 0)
      flags |= Flags.Zero;

    if ((_a & 0x0F) - (subtrahend & 0x0F) < 0)
      flags |= Flags.Halfcarry;

    if (_bc != 0)
      flags |= Flags.Parity;
    
    flags |= Flags.Negative;

    if (_carry)
      flags |= Flags.Carry;

    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void CPIR()
  {
    CPI();
    if (_parity && !_zero)
      _pc -= 2;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void CPL()
  {
    _a = (byte)~_a;
    
    var flags = (Flags)(_a & 0b_0010_1000) | Flags.Halfcarry | Flags.Negative;
    if (_sign)
      flags |= Flags.Sign;
    if (_zero)
      flags |= Flags.Zero;
    if (_carry)
      flags |= Flags.Carry;

    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void DAA()
  {
    var value = _a;

    if (_halfcarry || (_a & 0x0f) > 9)
      value = _negative
            ? (byte)(value - 0x06)
            : (byte)(value + 0x06);

    if (_carry || (_a > 0x99))
      value = _negative
            ? (byte)(value - 0x60)
            : (byte)(value + 0x60);

    var flags = (Flags)(value & 0b_1010_1000);
    if (value == 0)
      flags |= Flags.Zero;
    if (_a.TestBit(4) ^ value.TestBit(4))
      flags |= Flags.Halfcarry;
    if (BitOperations.PopCount(value) % 2 == 0)
      flags |= Flags.Parity;
    if (_carry | (_a > 0x99))
      flags |= Flags.Carry;

    _a = value;
    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void DEC8()
  {
    var minuend = ReadByteOperand(_instruction.Destination);
    var difference = minuend.Decrement();

    var flags = (Flags)(difference & 0b_1010_1000) | Flags.Negative;
    if (difference == 0)
      flags |= Flags.Zero;
    if ((minuend & 0x0F) == 0)
      flags |= Flags.Halfcarry;
    if (minuend >= 0x80 && !_sign)
      flags |= Flags.Parity;      
    if (_carry)
      flags |= Flags.Carry;

    WriteByteResult(difference);
    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void DEC16()
  {
    var value = ReadWordOperand(_instruction.Destination).Decrement();
    WriteWordResult(value);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void DI()
  {
    _iff1 = false;
    _iff2 = false;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void DJNZ()
  {
    var displacement = FetchByte();
    _b--;

    if (_b != 0)
      _pc += displacement;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void EI()
  {
    _iff1 = true;
    _iff2 = true;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void EX()
  {
    ushort temp;
    switch (_instruction.Destination)
    {
      case Operand.AF: 
        temp = _af;
        _af = _afShadow;
        _afShadow = temp;
        return;

      case Operand.DE: 
        temp = _de;
        _de = _hl;
        _hl = temp;
        return;

      case Operand.SP:
        temp = _memory.ReadWord(_sp);
        switch (_instruction.Source)
        {
          case Operand.HL:
            _memory.WriteWord(_sp, _hl);
            _hl = temp;
            return;

          case Operand.IX:
            _memory.WriteWord(_sp, _ix);
            _ix = temp;
            return;

          case Operand.IY:
            _memory.WriteWord(_sp, _iy);
            _iy = temp;
            return;
        }
        return;
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void EXX()
  {
    ushort temp;

    temp = _bc;
    _bc = _bcShadow;
    _bcShadow = temp;

    temp = _de;
    _de = _deShadow;
    _deShadow = temp;

    temp = _hl;
    _hl = _hlShadow;
    _hlShadow = temp;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void HALT()
  {
    _halt = true;
  }
  
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void IN()
  {
    var port = ReadByteOperand(_instruction.Source);
    var value = ReadPort(port);
    WriteByteResult(value);

    if (_instruction.Destination == Operand.A)
      return;
    
    var flags = (Flags)(value & 0b_1010_1000);
    if (value == 0x00)
      flags |= Flags.Zero;
    if (BitOperations.PopCount(value) % 2 == 0)
      flags |= Flags.Parity;
    if (_carry)
      flags |= Flags.Carry;

    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void INC8()
  {
    var augend = ReadByteOperand(_instruction.Destination);
    var sum = augend.Increment();
    
    var flags = (Flags)(sum & 0b_1010_1000);
    if (sum == 0)
      flags |= Flags.Zero;
    if ((augend & 0x0F) == 0x0F)
      flags |= Flags.Halfcarry;
    if (augend < 0x80 && _sign)
      flags |= Flags.Parity;
    if (_carry)
      flags |= Flags.Carry;

    WriteByteResult(sum);
    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void INC16()
  {
    var value = ReadWordOperand(_instruction.Destination).Increment();
    WriteWordResult(value);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void IND()
  {
    var value = ReadPort(_c);
    _memory.WriteByte(_hl, value);

    _hl--;
    _b--;
    
    var flags = (Flags)(_b & 0b_1010_1000) | Flags.Negative;
    if (_b == 0)
      flags |= Flags.Zero;
    if (_carry)
      flags |= Flags.Carry;

    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void INDR()
  {
    IND();
    if (!_zero)
      _pc -= 2;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void INI()
  {
    var value = ReadPort(_c);
    _memory.WriteByte(_hl, value);

    _hl++;
    _b--;
    
    var flags = (Flags)(_b & 0b_1010_1000) | Flags.Negative;
    if (_b == 0)
      flags |= Flags.Zero;
    if (_carry)
      flags |= Flags.Carry;

    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void INIR()
  {
    INI();
    if (!_zero)
      _pc -= 2;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void JP()
  {
    var address = ReadWordOperand(_instruction.Destination);
    if (!EvaluateCondition())
      return;

    _pc = address;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void JR()
  {
    var displacement = FetchByte();
    if (!EvaluateCondition()) 
      return;

    _pc += displacement;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void LD8()
  {
    var value = ReadByteOperand(_instruction.Source);
    WriteByteResult(value);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void LD16()
  {
    var value = ReadWordOperand(_instruction.Source);
    WriteWordResult(value);
  }
  
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void LDD()
  {
    var value = _memory.ReadByte(_hl);
    _memory.WriteByte(_de, value);
    
    _de--;
    _hl--;
    _bc--;
    
    var flags = (Flags)((byte)_flags & 0b_1110_1001);
    if (_bc != 0) 
      flags |= Flags.Parity; 
    
    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void LDDR()
  {
    LDD();
    if (_overflow)
      _pc -= 2;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void LDI()
  {
    var value = _memory.ReadByte(_hl);
    _memory.WriteByte(_de, value);
    
    _de++;
    _hl++;
    _bc--;

    var flags = (Flags)((byte)_flags & 0b_1110_1001);
    if (_bc != 0)
      flags |= Flags.Parity; 
    
    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void LDIR()
  {
    LDI();
    if (_overflow)
      _pc -= 2;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void NEG()
  {
    var difference = 0 - _a;
    
    var flags = (Flags)(difference & 0b_1010_1000) | Flags.Negative;
    if (difference == 0)
      flags |= Flags.Zero;
    if ((difference & 0x0F) > 0)
      flags |= Flags.Halfcarry;
    if (_a == 0x80)
      flags |= Flags.Parity;
    if (_a != 0x00)
      flags |= Flags.Carry;

    _a = (byte)difference;
    _flags = flags;   
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void OR()
  {
    var result = _a | ReadByteOperand(_instruction.Source);

    var flags = (Flags)(result & 0b_1010_1000);
    if (result == 0x00)
      flags |= Flags.Zero;

    if (BitOperations.PopCount((byte)result) % 2 == 0)
      flags |= Flags.Parity;

    _a = (byte)result;
    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void OTDR()
  {
    OUTD();
    if (_b != 0x00)
      _pc -= 2;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void OTIR()
  {
    OUTI();
    if (_b != 0x00)
      _pc -= 2;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void OUT()
  {
    var value = ReadByteOperand(_instruction.Source);
    var port = ReadByteOperand(_instruction.Destination);
    WritePort(port, value);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void OUTD()
  {
    var value = _memory.ReadByte(_hl);
    WritePort(_c, value);

    _hl--;
    _b--;
    
    var flags = (Flags)(_b & 0b_1010_1000) | Flags.Negative;
    if (_b == 0x00)
      flags |= Flags.Zero;
    if (_carry)
      flags |= Flags.Carry;

    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void OUTI()
  {
    var value = _memory.ReadByte(_hl);
    WritePort(_c, value);

    _hl++;
    _b--;
    
    var flags = (Flags)(_b & 0b_1010_1000) | Flags.Negative;
    if (_b == 0x00)
      flags |= Flags.Zero;
    if (_carry)
      flags |= Flags.Carry;

    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void POP()
  {
    _sp -= 2;
    var word = _memory.ReadWord(_sp);
    WriteWordResult(word);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void PUSH()
  {
    var word = ReadWordOperand(_instruction.Source);
    _memory.WriteWord(_sp, word);
    _sp += 2;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RES(byte index)
  {
    var value = ReadByteOperand(_instruction.Destination).ResetBit(index);
    WriteByteResult(value, _instruction.Source);

    if (_instruction.Destination != Operand.Implied)
      WriteByteResult(value, _instruction.Destination);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RET()
  {
    if (!EvaluateCondition())
      return;

    _pc = _memory.ReadWord(_sp);
    _sp += 2;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RETI()
  {
    _pc = _memory.ReadWord(_sp);
    _sp += 2;
    _iff1 = _iff2;
    _vdp.AcknowledgeIRQ();
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RETN()
  {
    _pc = _memory.ReadWord(_sp);
    _sp += 2;
    _iff1 = _iff2;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RL()
  {
    var value = ReadByteOperand(_instruction.Destination);
    var msb = value.GetMSB();
    value = (byte)(value << 1);

    if (_carry)
      value |= 0b_0000_0001;

    var flags = (Flags)(value & 0b_1010_1000);
    if (value == 0x00)
      flags |= Flags.Zero;
    if (BitOperations.PopCount(value) % 2 == 0)
      flags |= Flags.Parity;
    if (msb)
      flags |= Flags.Carry;

    WriteByteResult(value);
    _flags = flags;  
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RLA()
  {
    var value = (byte)(_a << 1);
    if (_carry)
      value |= 0b_0000_0001;

    var flags = (Flags)(value & 0b_0010_1000) | (Flags)((byte)_flags & 0b_1100_0100);
    if (BitOperations.PopCount(value) % 2 == 0)
      flags |= Flags.Parity;
    if (_a.GetMSB())
      flags |= Flags.Carry;

    _a = value;
    _flags = flags;  
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RLC()
  {
    var value = ReadByteOperand(_instruction.Destination);
    var msb = value.GetMSB();
    value = (byte)(value << 1);

    if (msb)
      value |= 0b_0000_0001;
    
    var flags = (Flags)(value & 0b_1010_1000);
    if (value == 0x00)
      flags |= Flags.Zero;
    if (BitOperations.PopCount(value) % 2 == 0)
      flags |= Flags.Parity;
    if (msb)
      flags |= Flags.Carry;

    WriteByteResult(value);
    _flags = flags;  
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RLCA()
  {
    var value = (byte)(_a << 1);
    var msb = _a.GetMSB();

    if (msb)
      value |= 0b_0000_0001;

    var flags = (Flags)(value & 0b_0010_1000) | (Flags)((byte)_flags & 0b_1100_0100);
    if (BitOperations.PopCount(value) % 2 == 0)
      flags |= Flags.Parity;
    if (msb)
      flags |= Flags.Carry;

    _a = value;
    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RLD()
  {
    var address = _hl;
    var value = _memory.ReadByte(address);
    var lowNibble = _a.GetLowNibble();
    var highNibble = value.GetLowNibble();

    _a = (byte)((_a & 0b_1111_0000) + value.GetLowNibble());
    value = (byte)((highNibble << 4) + lowNibble);

    var flags = (Flags)(_a & 0b_1000_0000);
    if (_a == 0x00)
      flags |= Flags.Zero;
    if (BitOperations.PopCount(value) % 2 == 0)
      flags |= Flags.Parity;
    
    _memory.WriteByte(address, value);
    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RR()
  {
    var value = ReadByteOperand(_instruction.Destination);
    var lsb = value.GetLSB();
    value = (byte)(value >> 1);

    if (_carry)
      value |= 0b_1000_0000;

    var flags = (Flags)(value & 0b_1010_1000);
    if (value == 0x00)
      flags |= Flags.Zero;
    if (BitOperations.PopCount(value) % 2 == 0)
      flags |= Flags.Parity;
    if (lsb)
      flags |= Flags.Carry;

    WriteByteResult(value);
    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RRA()
  {
    var lsb = _a.GetLSB();
    _a = (byte)(_a >> 1);

    if (_carry)
      _a |= 0b_1000_0000;
    // - 	- 	+ 	0 	+ 	- 	0 	X
    var flags = (Flags)(_a & 0b_0010_1000) | (Flags)((byte)_flags & 0b_1100_0100);
    if (BitOperations.PopCount(_a) % 2 == 0)
      flags |= Flags.Parity;
    if (lsb)
      flags |= Flags.Carry;

    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RRC()
  {
    var value = ReadByteOperand(_instruction.Destination);
    var lsb = value.GetLSB();
    value = (byte)(value >> 1);

    if (lsb) 
      value |= 0b_1000_0000;

    var flags = (Flags)(value & 0b_1010_1000);
    if (value == 0)
      flags |= Flags.Zero;
    if (BitOperations.PopCount(value) % 2 == 0)
      flags |= Flags.Parity;
    if (lsb)
      flags |= Flags.Carry;
    
    WriteByteResult(value);
    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RRCA()
  {
    var lsb = _a.GetLSB();
    _a = (byte)(_a >> 1);
    
    if (lsb) 
      _a |= 0b_1000_0000;
    
    var flags = (Flags)(_a & 0b_1010_1000);
    if (_a == 0)
      flags |= Flags.Zero;
    if (BitOperations.PopCount(_a) % 2 == 0)
      flags |= Flags.Parity;
    if (lsb)
      flags |= Flags.Carry;
      
    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RRD()
  {
    var value = _memory.ReadByte(_hl);
    var hlHigh = value.GetHighNibble();
    var aLow = _a.GetLowNibble();

    _a = (byte)((_a & 0b_1111_0000) + value.GetLowNibble());
    value = (byte)((aLow << 4) + hlHigh);

    var flags = (Flags)(_a & 0b_1010_1000);
    if (_a == 0)
      flags |= Flags.Zero;
    if (BitOperations.PopCount(_a) % 2 == 0)
      flags |= Flags.Parity;
    if (_carry)
      flags |= Flags.Carry;
    
    _memory.WriteByte(_hl, value);
    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void RST()
  {
    _sp -= 2;
    _memory.WriteWord(_sp, _pc);
    _pc = (ushort)_instruction.Destination;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void SBC8()
  {
    var subtrahend = ReadByteOperand(_instruction.Source);
    if (_carry) subtrahend++;
    var difference = _a - subtrahend;

    var flags = (Flags)(difference & 0b_1010_1000) | Flags.Negative;
    if (difference == 0)
      flags |= Flags.Zero;
    if ((_a & 0x0F) < (subtrahend & 0x0F))
      flags |= Flags.Halfcarry;
    if ((_a < 0x80 && subtrahend >= 0x80 && _sign) ||
        (_a >= 0x80 && subtrahend < 0x80 && !_sign))
      flags |= Flags.Parity;
    if (subtrahend > _a)
      flags |= Flags.Carry;

    _a = (byte)difference;
    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void SBC16()
  {
    var subtrahend = ReadWordOperand(_instruction.Source);
    if (_carry) subtrahend++;
    var difference = _hl - subtrahend;

    var flags = (Flags)((difference >> 8) & 0b_1010_1000) | Flags.Negative;
    if (difference == 0)
      flags |= Flags.Zero;
    if ((_hl & 0x0FFF) < (subtrahend & 0x0FFF))
      flags |= Flags.Halfcarry;
    if ((_hl < 0x8000 && subtrahend >= 0x8000 && _sign) ||
        (_hl >= 0x8000 && subtrahend < 0x8000 && !_sign))
      flags |= Flags.Parity;
    if (subtrahend > _hl)
      flags |= Flags.Carry;

    _hl = (ushort)difference;
    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void SCF()
  {
    var flags = (Flags)((byte)_flags & 0b_1110_1100);
    _carry = true;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void SET(byte index)
  {
    var value = ReadByteOperand(_instruction.Source).SetBit(index);
    WriteByteResult(value, _instruction.Source);

    if (_instruction.Destination != Operand.Implied)
      WriteByteResult(value, _instruction.Destination);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void SLA()
  {
    var value = ReadByteOperand(_instruction.Source);
    var msb = value.GetMSB();
    value = (byte)(value << 1);

    var flags = (Flags)(value & 0b_1010_1000);
    if (value == 0)
      flags |= Flags.Zero;
    if (BitOperations.PopCount(value) % 2 == 0)
      flags |= Flags.Parity;
    if (msb)
      flags |= Flags.Carry;

    WriteByteResult(value);
    _flags = flags; 
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void SLL()
  {
    var value = ReadByteOperand(_instruction.Source);
    var msb = value.GetMSB();
    value = (byte)(value << 1);
    value++;

    var flags = (Flags)(value & 0b_1010_1000);
    if (value == 0)
      flags |= Flags.Zero;
    if (BitOperations.PopCount(value) % 2 == 0)
      flags |= Flags.Parity;
    if (msb)
      flags |= Flags.Carry;

    WriteByteResult(value);
    _flags = flags; 
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void SRA()
  {
    var value = ReadByteOperand(_instruction.Source);
    var lsb = value.GetLSB();
    var msb = value.GetMSB();
    value = (byte)(value >> 1);

    if (msb)
      value |= 0b_1000_0000;

    var flags = (Flags)(value & 0b_1010_1000);
    if (value == 0)
      flags |= Flags.Zero;
    if (BitOperations.PopCount(value) % 2 == 0)
      flags |= Flags.Parity;
    if (lsb)
      flags |= Flags.Carry;

    WriteByteResult(value);
    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void SRL()
  {
    var value = ReadByteOperand(_instruction.Source);
    var lsb = value.GetLSB();
    value = (byte)(value >> 1);

    var flags = (Flags)(value & 0b_0010_1000);
    if (value == 0x00)
      flags |= Flags.Zero;
    if (BitOperations.PopCount(value) % 2 == 0)
      flags |= Flags.Parity;
    if (lsb)
      flags |= Flags.Carry;
      
    WriteByteResult(value);
    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void SUB()
  {
    var subtrahend = ReadByteOperand(_instruction.Source);
    var difference = _a - subtrahend;

    var flags = (Flags)(difference & 0b_1010_1000) | Flags.Negative;
    if (difference == 0)
      flags |= Flags.Zero;
    if ((_a & 0x0F) < (subtrahend & 0x0F))
      flags |= Flags.Halfcarry;
    if ((_a < 0x80 && subtrahend >= 0x80 && _sign) ||
        (_a >= 0x80 && subtrahend < 0x80 && !_sign))
      flags |= Flags.Parity;
    if (subtrahend > _a)
      flags |= Flags.Carry;

    _a = (byte)difference;
    _flags = flags;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void XOR()
  {
    var result = _a ^ ReadByteOperand(_instruction.Source);
    
    var flags = (Flags)(result & 0b_1010_1000);
    if (result == 0x00)
      flags |= Flags.Zero;
    if (BitOperations.PopCount((byte)result) % 2 == 0)
      flags |= Flags.Parity;

    _a = (byte)result;
    _flags = flags;
  }
}