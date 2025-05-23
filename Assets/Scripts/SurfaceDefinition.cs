using UnityEngine;

public class SurfaceDefinition : MonoBehaviour
{
    public SurfaceType SurfaceType;
}

public enum SurfaceType
{
    Wood,           // – Hardwood, laminate, parquet.
    Tile,           // – Ceramic, porcelain (common in kitchens, bathrooms).
    Carpet,         // – Soft and muffled footstep sound.    
    Concrete,       // – Found in basements, modern designs.        
    Wet,            // - Bathroom, spill of liquid
    Fragile         // - Glass shatters
}