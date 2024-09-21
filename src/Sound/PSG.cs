﻿using Quill.Common;
using System;
using System.Diagnostics;
using System.Threading;

namespace Quill.Sound;

public sealed class PSG
{
  #region Constants
  private const int SAMPLE_RATE = 48000;
  private const int BUFFER_SIZE = 4800;
  private const double UPDATE_INTERVAL_MS = 1d;
  private const int SAMPLES_PER_UPDATE = 48;
  private const int MAX_VOLUME = 8191;
  private const int VOLUME_LEVELS = 16;
  private const double VOLUME_STEP = 0.79432823F;
  private const int CHANNEL_COUNT = 4;
  private const int PULSE0 = 0b_00;
  private const int PULSE1 = 0b_01;
  private const int PULSE2 = 0b_10;
  private const int NOISE = 0b_11;
  #endregion

  #region Fields
  private readonly Thread _bufferThread;
  private readonly byte[] _buffer;
  private readonly bool _littleEndian;
  private int _bufferPosition;

  private readonly Channel[] _channels;
  private readonly double[] _volumes;
  private int _latchedChannel;
  private bool _isVolumeLatched;
  private bool _playing;
  #endregion

  public PSG()
  {
    _channels = new Channel[CHANNEL_COUNT];
    _channels[PULSE0] = new Channel();
    _channels[PULSE1] = new Channel();
    _channels[PULSE2] = new Channel();
    _channels[NOISE] = new Channel();

    _littleEndian = BitConverter.IsLittleEndian;
    _bufferPosition = 0;
    _buffer = new byte[BUFFER_SIZE];
    _volumes = new double[VOLUME_LEVELS];
    double volume = MAX_VOLUME;
    for (int i = 0; i < 15; i++)
    {
      _volumes[i] = volume;
      volume *= VOLUME_STEP;
    }

    _playing = true;
    _bufferThread = new Thread(UpdateBuffer);
    _bufferThread.Start();
  }

  #region Methods
  public void UpdateBuffer()
  {
    var clock = new Stopwatch();
    var lastUpdate = 0d;
    clock.Start();

    while (_playing)
    {
      if (_bufferPosition == BUFFER_SIZE)
        continue;

      var currentTime = clock.Elapsed.TotalMilliseconds;
      if (currentTime < lastUpdate + UPDATE_INTERVAL_MS)
        continue;

      lock (_buffer)
      {
        for (int i = 0; i < SAMPLES_PER_UPDATE; i++)
        {
          double tone = 0;
          for (int index = 0; index < CHANNEL_COUNT; index++)
          {
            if (index == NOISE)
              break; // TODO

            var pulse = _channels[index];
            if (pulse.Tone == 0)
              continue;

            pulse.Counter--;

            if (pulse.Counter == 0)
            {
              pulse.Counter = pulse.Tone;
              pulse.Polarity = !pulse.Polarity;
            }

            if (pulse.Polarity)
              tone += _volumes[pulse.Volume];
            else
              tone -= _volumes[pulse.Volume];

            _channels[index] = pulse;
          }
          
          var frame = (short)tone;
          if (_littleEndian)
          {
            _buffer[_bufferPosition++] = (byte)frame;
            _buffer[_bufferPosition++] = (byte)(frame >> 8);
          }
          else
          {
            _buffer[_bufferPosition++] = (byte)(frame >> 8);
            _buffer[_bufferPosition++] = (byte)frame;
          }
        }
      }
    }
  }

  public void Play()
  {
    _playing = true;
    _bufferThread.Start();
  }

  public void Stop() => _playing = false;

  public void WriteData(byte value)
  {
    if (value.TestBit(7))
    {
      _latchedChannel = (value >> 5) & 0b_11;
      _isVolumeLatched = value.TestBit(4);

      if (_isVolumeLatched)
      {
        _channels[_latchedChannel].Volume = value.LowNibble();
      }
      else
      {
        _channels[_latchedChannel].Tone &= 0b_0011_1111_0000;
        _channels[_latchedChannel].Tone |= value.LowNibble();
      }
    }
    else
    {
      if (_isVolumeLatched)
      {
        _channels[_latchedChannel].Volume = value.LowNibble();
      }
      else
      {
        if (_latchedChannel == NOISE)
        {
          _channels[NOISE].Tone = value.LowNibble();
          return;
        }

        _channels[_latchedChannel].Tone &= 0b_0000_0000_1111;
        _channels[_latchedChannel].Tone |= (ushort)((value & 0b_0011_1111) << 4);
      }
    }
  }

  public byte[] ReadAudioBuffer()
  {
    lock (_buffer)
    {
      var result = new byte[_bufferPosition];
      Array.Copy(_buffer, result, _bufferPosition);
      _bufferPosition = 0;
      return result;
    }
  }
  #endregion
}
