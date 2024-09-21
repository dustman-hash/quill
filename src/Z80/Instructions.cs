using Quill.Definitions;
using Quill.Extensions;

namespace Quill.Z80
{
  public partial class CPU
  {
    private void ADC()
    {
      if (_cir.Destination == Operand.HL)
      {
        var result = _reg.HL + GetWordOperand(_cir.Source);
        if ((result >> 12) > ushort.MaxValue)
        {
          //_reg.SetFlag()
        }
        return;
      }
      
      var result = _reg.A + GetByteOperand(_cir.Source);

      if (_reg.Flags.HasFlag(Flags.Carry))
        result++;

      //_reg.SetFlag(Flags.S, )
    }

    private void ADD()
    {

    }

    private void AND()
    {

    }
    
    private void BIT()
    {
      
    }

    
    private void CALL()
    {
      
    }

    private void CCF()
    {

    }

    private void CP()
    {
      
    }

    private void CPD()
    {
      
    }

    private void CPI()
    {
      
    }

    private void CPIR()
    {
      
    }

    private void CPL()
    {
      
    }

    private void DAA()
    {
      
    }

    private void DEC()
    {
      
    }

    private void DI()
    {
      
    }

    private void DJNZ()
    {
      
    }

    private void EI()
    {
      
    }

    private void EX()
    {
      
    }

    private void EXX()
    {
      
    }

    private void EN()
    {
      
    }
 
    private void HALT()
    {
      
    }

    private void IM()
    {
      
    }
 
    private void IN()
    {
      
    }

    private void INC()
    {
      
    }

    private void IND()
    {
      
    }

    private void INI()
    {
      
    }

    private void INIR()
    {
      
    }

    private void JP()
    {
      
    }

    private void JR()
    {
      
    }

    private void LD()
    {
      
    }

    private void NEG()
    {
      
    }

    private void NOP()
    {
      
    }

    private void OR()
    {
      
    }

    private void OTDR()
    {
      
    }

    private void OTIR()
    {
      
    }

    private void OUT()
    {
      
    }

    private void OUTD()
    {
      
    }

    private void OUTI()
    {
      
    }

    private void POP()
    {
      
    }

    private void PUSH()
    {
      
    }

    private void RES()
    {
      
    }

    private void RL()
    {
      
    }

    private void RLA()
    {
      
    }

    private void RLC()
    {
      
    }

    private void RLCA()
    {
      
    }

    private void RLD()
    {
      
    }

    private void RR()
    {
      
    }

    private void RRA()
    {
      
    }

    private void RRC()
    {
      
    }

    private void RRCA()
    {
      
    }

    private void RRD()
    {
      
    }

    private void RST()
    {
      
    }

    private void SBC()
    {
      
    }

    private void SCF()
    {

    }

    private void SET()
    {

    }

    private void SLA()
    {

    }

    private void SRA()
    {

    }

    private void SRL()
    {

    }

    private void SUB()
    {

    }

    private void XOR()
    {

    }
  }
}