#pragma once

uint4 SampleBlitTexture(uint2 baseOffset, uint2 windowOffset)
{
    uint2 coords = baseOffset + windowOffset;
    uint4 sample = _BlitTexture[coords];
        
    uint4 dest = (uint4) 0;
    if (_R != BLIT_CHANNEL_NONE)
        dest.r = sample[_R];
    if (_G != BLIT_CHANNEL_NONE)
        dest.g = sample[_G];
    if (_B != BLIT_CHANNEL_NONE)
        dest.b = sample[_B];
    if (_A != BLIT_CHANNEL_NONE)
        dest.a = sample[_A];
    
    return dest;
}

uint4 SampleBlitTexture()
{
    return SampleBlitTexture(uint2(_BlitParams.xy), uint2(_BlitParams.zw));
}