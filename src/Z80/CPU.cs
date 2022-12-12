using System.Runtime.CompilerServices;
using Quill.Extensions;
using static Quill.Z80.Opcodes;

namespace Quill.Z80
{
  public partial class CPU
  {
    private Memory _memory;
    private ushort _memPtr;
    private bool _nmiRequested;
    private bool _intRequested;
    private int _cycleCount;
    private int _instructionCount;

    public CPU()
    {
      _memory = new Memory();
    }

    public void LoadProgram(byte[] rom)
    {
      for (ushort index = 0x00; index < rom.Count(); index++)
        _memory.WriteByte(index, rom[index]);
    }

    public void Step()
    {
      HandleInterrupts();
      FetchInstruction();
      ExecuteInstruction();
      _instructionCount++;
    }

    public void RequestINT()
    {
      // TODO
    }
    
    public void RequestNMI()
    {
      // TODO
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void HandleInterrupts()
    {
      // TODO
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void FetchInstruction()
    {
      var op = FetchByte();
      _instruction = op switch
      {
        0xCB  =>  DecodeCBInstruction(),
        0xDD  =>  DecodeDDInstruction(),
        0xED  =>  DecodeEDInstruction(),
        0xFD  =>  DecodeFDInstruction(),
        _     =>  Opcodes.Main[op]
      };
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Opcode DecodeCBInstruction()
    {
      return Opcodes.CB[FetchByte()];
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Opcode DecodeDDInstruction()
    {
      var op = FetchByte();

      if (op != 0xCB)
        return Opcodes.DD[op];
      
      op = FetchByte();
      return Opcodes.DDCB[op];
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Opcode DecodeEDInstruction()
    {
      return Opcodes.ED[FetchByte()];
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Opcode DecodeFDInstruction()
    {
      var op = FetchByte();

      if (op != 0xCB)
        return Opcodes.FD[op];
        
      op = FetchByte();
      return Opcodes.FDCB[op];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExecuteInstruction()
    {
      switch (_instruction.Operation)
      {
        case Operation.ADC:   ADC();  break;
        case Operation.ADD:   ADD();  break;
        case Operation.AND:   AND();  break;
        case Operation.BIT:   BIT();  break;
        case Operation.CALL:  CALL(); break;
        case Operation.CCF:   CCF();  break;
        case Operation.CP:    CP();   break;
        case Operation.CPD:   CPD();  break;
        case Operation.CPI:   CPI();  break;
        case Operation.CPIR:  CPIR(); break;
        case Operation.CPL:   CPL();  break;
        case Operation.DAA:   DAA();  break;
        case Operation.DEC:   DEC();  break;
        case Operation.DI:    DI();   break;
        case Operation.DJNZ:  DJNZ(); break;
        case Operation.EI:    EI();   break;
        case Operation.EX:    EX();   break;
        case Operation.EXX:   EXX();  break;
        case Operation.HALT:  HALT(); break;
        case Operation.IM:    IM();   break;
        case Operation.IN:    IN();   break;
        case Operation.INC:   INC();  break;
        case Operation.IND:   IND();  break;
        case Operation.INI:   INI();  break;
        case Operation.INIR:  INIR(); break;
        case Operation.JP:    JP();   break;
        case Operation.JR:    JR();   break;
        case Operation.LD:    LD();   break;
        case Operation.NEG:   NEG();  break;
        case Operation.NOP:   NOP();  break;
        case Operation.OR:    OR();   break;
        case Operation.OTDR:  OTDR(); break;
        case Operation.OTIR:  OTIR(); break;
        case Operation.OUT:   OUT();  break;
        case Operation.OUTD:  OUTD(); break;
        case Operation.OUTI:  OUTI(); break;
        case Operation.POP:   POP();  break;
        case Operation.PUSH:  PUSH(); break;
        case Operation.RES:   RES();  break;
        case Operation.RL:    RL();   break;
        case Operation.RLA:   RLA();  break;
        case Operation.RLC:   RLC();  break;
        case Operation.RLCA:  RLCA(); break;
        case Operation.RLD:   RLD();  break;
        case Operation.RR:    RR();   break;
        case Operation.RRA:   RRA();  break;
        case Operation.RRC:   RRC();  break;
        case Operation.RRCA:  RRCA(); break;
        case Operation.RRD:   RRD();  break;
        case Operation.RST:   RST();  break;
        case Operation.SBC:   SBC();  break;
        case Operation.SCF:   SCF();  break;
        case Operation.SET:   SET();  break;
        case Operation.SLA:   SLA();  break;
        case Operation.SRA:   SRA();  break;
        case Operation.SRL:   SRL();  break;
        case Operation.SUB:   SUB();  break;
        case Operation.XOR:   XOR();  break;
      }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private byte FetchByte() => _memory.ReadByte(_pc++);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ushort FetchWord()
    {
      var lowByte = FetchByte();
      var highByte = FetchByte();
      return highByte.Concat(lowByte);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private byte ReadByte(Operand operand)
    {
      switch (operand)
      {
        case Operand.Indirect:
          _memPtr = FetchWord();
          break;

        case Operand.BCi:
        case Operand.DEi:
        case Operand.HLi:
          _memPtr = ReadRegisterPair(operand);
          break;

        case Operand.IXd:
        case Operand.IYd:
          _memPtr = (byte)(ReadRegister(operand) + FetchByte());
          break;

        case Operand.Immediate:
          return FetchByte();

        case Operand.A:
        case Operand.B:
        case Operand.C:
        case Operand.D:
        case Operand.E:
        case Operand.F:
        case Operand.H:
        case Operand.L:
          return ReadRegister(operand);
        
        default: throw new InvalidOperationException();
      }
      return _memory.ReadByte(_memPtr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ushort ReadWord(Operand operand)
    {
      switch (operand)
      {
        case Operand.Indirect:
          _memPtr = FetchWord();
          break;

        case Operand.Immediate:
          return FetchWord();

        case Operand.AF:
        case Operand.BC: 
        case Operand.DE: 
        case Operand.HL: 
        case Operand.IX:  
        case Operand.IY: 
        case Operand.PC:
        case Operand.SP:
          return ReadRegisterPair(operand);

        default:
          return 0x00;
      }
      return _memory.ReadByte(_memPtr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteByte(byte value)
    {
      switch (_instruction.Destination)
      {
        case Operand.A: _a = value; return;
        case Operand.B: _b = value; return;
        case Operand.C: _c = value; return;
        case Operand.D: _d = value; return;
        case Operand.E: _e = value; return;
        case Operand.F: _f = value; return;
        case Operand.H: _h = value; return;
        case Operand.L: _l = value; return;

        case Operand.Indirect:  
          _memPtr = FetchWord();
          break;

        case Operand.IXd:
          _memPtr = (byte)(_ix + FetchByte());
          break;

        case Operand.IYd:
          _memPtr = (byte)(_iy + FetchByte());
          break;

        case Operand.BCi: _memPtr = _bc; break;
        case Operand.DEi: _memPtr = _de; break;
        case Operand.HLi: _memPtr = _hl; break;

        default: throw new InvalidOperationException();
      }
      _memory.WriteByte(_memPtr, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteWord(ushort value)
    {
      switch (_instruction.Destination)
      {
        case Operand.Indirect:
          _memory.WriteWord(_memPtr, value);
          return;

        case Operand.AF: _af = value; return;
        case Operand.BC: _bc = value; return;
        case Operand.DE: _de = value; return;
        case Operand.HL: _hl = value; return;
        case Operand.IX: _ix = value; return;
        case Operand.IY: _iy = value; return;
        case Operand.PC: _pc = value; return;
        case Operand.SP: _sp = value; return;

        default: throw new InvalidOperationException();
      }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool EvaluateCondition()
    {
      return _instruction.Source switch
      {
        Operand.Carry     => _carry,
        Operand.NonCarry  => !_carry,
        Operand.Zero      => _zero,
        Operand.NonZero   => !_zero,
        Operand.Negative  => _sign,
        Operand.Positive  => !_sign,
        Operand.Even      => _parity,
        Operand.Odd       => !_parity,
        _                 => true
      };
    }

    public void DumpMemory() => _memory.DumpPage(0x00);

    public override String ToString() => DumpRegisters() +
      $"Instruction: {_instructionCount}, Flags: {_flags.ToString()}\r\n ";
  }
}