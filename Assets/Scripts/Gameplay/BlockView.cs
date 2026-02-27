using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BlockView : MonoBehaviour
{
    [SerializeField]
    private GameObject visualObject;
    public WaterController fillWater; // optional (Image type = Filled)
    public float moveDuration = 0.15f;
    public GameObject trayObject;
    public MeshRenderer[] trayRenderer;
    public GameObject glassObject;
    public MeshRenderer[] glassRenderer;
    public bool pouring = false;
    public Block block;
    public Rigidbody2D rigidBody;
    [SerializeField]
    private InnerBlock innerBlock;

    [SerializeField]
    private GameObject iceObject;
    [SerializeField]
    private TextMeshPro iceText;
    [SerializeField]
    private GameObject stoneObject;

    private int _iceCount;
    public int iceCount
    {
        get
        {
            return _iceCount;
        }
        set
        {
            _iceCount = value;
            iceText.text = value.ToString();
            //Add some visual effect here
            if (value <= 0)
            {
                iceObject.SetActive(false);
                visualObject.SetActive(true);
            }
        }
    }

    private bool _isStone;
    public bool isStone
    {
        get
        {
            return _isStone;
        }
        set
        {
            _isStone = value;
            stoneObject.SetActive(value);
            visualObject.SetActive(!value);
            iceObject.SetActive(!value);
        }
    }

    public bool hasInnerBlock
    {
        get
            {
            return block.data.innerBlockColor >= 0;
        }
        set
        {
            
        }
    }

    [SerializeField]
    private GameObject colliderObject;


    public void ResetBlock()
    {
        pouring = false;
        transform.localScale = Vector3.one;
        fillWater.ResetWater();
        innerBlock.ResetInnerBlock();
        rigidBody.bodyType = RigidbodyType2D.Static;
    }    

    public void SetData(Block bl)
    {
        this.block = bl;
        SetBlockColor();
        SetInnerBlock();
        isStone = block.data.isStone;
        if(isStone == false)
        {
            SetIceBlock();
        }    
        
            
        if (rigidBody == null)
        {
            rigidBody = GetComponent<Rigidbody2D>();
        }
        // TODO: apply color / shape mesh here
    }

    private void SetIceBlock()
    {
        iceCount = block.data.iceCount;
        if(iceCount <= 0)
        {
            iceObject.SetActive(false);
            visualObject.SetActive(true);
            return;
        }
        visualObject.SetActive(false);
        iceObject.SetActive(true);
        float rotZ = block.data.rotation;
        iceText.rectTransform.localEulerAngles = new Vector3(0, 180f, -rotZ);
    }    
    public void SetInnerBlock()
    {
        int innerColor = block.data.innerBlockColor;
        if (innerColor < 0)
        {
            glassObject.SetActive(true);
            return;
        }
        glassObject.SetActive(false);
        Color color = DataContainer.Instance.blockColorData.GetColor((ColorEnum)innerColor);
        float dur = block.innerCapacityUnits * GlobalValues.fruitSqueezeOutDuration;
        innerBlock.SetInnerBlock(color, block.index, dur, block.data.innerBlockColor);
    }    
    public void OnFillWaterComplete(bool isComplete, bool isInner)
    {
        // Handle fill water complete event here
        pouring = false;
        
        if (isComplete)
        {
            // Additional logic when block is completely filled
            GameController.Instance.boardView.HandleBlockFull();
            if (isInner)
            {
                InnerBlockComplete();
            }
            else
            {
                BlockFillComplete();
            }    
            
        }
    }

    private void InnerBlockComplete()
    {
        glassObject.SetActive(true);
        innerBlock.BlockComplete();
    }    
    private void BlockFillComplete()
    {
        transform.DOScale(Vector3.zero, 0.3f).OnComplete(() =>
        {
            DisableBlockView();
        });
    }  
    
    private void DisableBlockView()
    {
        gameObject.SetActive(false);
    }    
    private void SetBlockColor()
    {
        
        if (block.data.isMixColor)
        {
            // Mixed color handling
            for(int i = 0; i < block.data.mixColors.Count; i++)
            {
                var colorData = block.data.mixColors[i];
                Color mixColor = DataContainer.Instance.blockColorData.GetColor((ColorEnum)colorData.color);
                trayRenderer[i].material.color = mixColor;
                glassRenderer[i].material.color = new Color(mixColor.r, mixColor.g, mixColor.b, 0.6f);
                float dur = colorData.colorCount * GlobalValues.fruitSqueezeOutDuration;
                fillWater.SetMixWaterColor(mixColor, block.index, dur, colorData.color,i);
            }
        }
        else
        {
            Color color = DataContainer.Instance.blockColorData.GetColor((ColorEnum)block.data.color);
            Color glass = new Color(color.r, color.g, color.b, 0.6f);
            foreach (var tray in trayRenderer)
            {
                tray.material.color = color;
            }
            foreach (var glassR in glassRenderer)
            {
                glassR.material.color = glass;
            }
            float duration = block.capacityUnits * GlobalValues.fruitSqueezeOutDuration;
            fillWater.SetWaterColor(color, block.index, duration, block.data.color);
        }   
        
    }    



    public void Pour(float taken, bool isComplete, int color, Vector3 pourWorldPos)
    {
        pouring = true;
        
        if(block.data.isMixColor)
        {
            float pourAmount = taken * 1f / block.capacitiesPerColor[color].capacityUnits;
            fillWater.SetMixFillAmount(pourAmount, color, isComplete, pourWorldPos);
        }
        else
        {
            float pourAmount = taken * 1f / block.capacityUnits;
            fillWater.SetFillAmount(pourAmount, isComplete, pourWorldPos);
        }    
    }    
    public void PourInner(float taken, bool isComplete, Vector3 pourWorldPos)
    {
        pouring = true;
        float pourAmount = taken * 1f / block.innerCapacityUnits;
        innerBlock.PourInner(pourAmount, isComplete, pourWorldPos);
    }



}
