class Pcm16kCaptureProcessor extends AudioWorkletProcessor {
  constructor() {
    super();
    this._buffer = new Int16Array(1600); // 100ms at 16kHz
    this._offset = 0;
  }

  process(inputs) {
    const input = inputs[0];
    if (!input || !input[0]) return true;

    const channelData = input[0];
    for (let i = 0; i < channelData.length; i++) {
      const sample = Math.max(-1, Math.min(1, channelData[i]));
      this._buffer[this._offset++] = sample < 0 ? sample * 0x8000 : sample * 0x7fff;

      if (this._offset >= this._buffer.length) {
        const chunkCopy = new Int16Array(this._buffer);
        this.port.postMessage({ pcmBuffer: chunkCopy.buffer }, [chunkCopy.buffer]);
        this._offset = 0;
      }
    }
    return true;
  }
}

registerProcessor('pcm16k-capture-processor', Pcm16kCaptureProcessor);
