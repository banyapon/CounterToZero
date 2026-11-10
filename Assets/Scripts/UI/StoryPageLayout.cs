using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CountToZero
{
    // Shared by the authored scenes and runtime, so anchors remain consistent.
    public static class StoryPageLayout
    {
        static RectTransform Child(Transform parent,string name)
        {
            var existing=parent.Find(name) as RectTransform;
            return existing!=null?existing:CampaignExpansionUI.Rect(name,parent,Vector2.zero,Vector2.zero);
        }
        static void Place(RectTransform rect,Transform parent,Vector2 min,Vector2 max,Vector2 insetMin,Vector2 insetMax)
        {
            rect.SetParent(parent,false);rect.localScale=Vector3.one;rect.localRotation=Quaternion.identity;
            rect.pivot=Vector2.one*.5f;rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=insetMin;rect.offsetMax=insetMax;
        }
        static Image Backing(RectTransform rect,Color color)
        {
            var image=rect.GetComponent<Image>();if(image==null)image=rect.gameObject.AddComponent<Image>();
            image.sprite=null;image.color=color;image.raycastTarget=false;return image;
        }
        public static Image Arrange(CampaignScreen screen)
        {
            var canvas=screen.PrimaryButton.GetComponentInParent<Canvas>().transform;
            var root=Child(canvas,"Story Layout");
            Place(root,canvas,Vector2.zero,Vector2.one,new Vector2(32,40),new Vector2(-32,-40));
            var left=Child(root,"Artwork 60 Percent");
            Place(left,root,Vector2.zero,new Vector2(.6f,1),Vector2.zero,new Vector2(-12,0));
            Backing(left,new Color(.02f,.04f,.07f));
            var art=Child(left,"Aseprite comic");
            Place(art,left,Vector2.zero,Vector2.one,new Vector2(8,8),new Vector2(-8,-8));
            var image=Backing(art,Color.white);image.preserveAspect=true;
            var right=Child(root,"Narrative 40 Percent");
            Place(right,root,new Vector2(.6f,0),Vector2.one,new Vector2(12,0),Vector2.zero);
            Backing(right,new Color(.025f,.05f,.085f));
            var accent=Child(right,"Cyan Divider");
            Place(accent,right,Vector2.zero,new Vector2(0,1),Vector2.zero,new Vector2(3,0));
            Backing(accent,ArenaVisuals.Cyan);
            var heading=screen.Heading;
            Place(heading.rectTransform,right,new Vector2(0,1),Vector2.one,new Vector2(28,-116),new Vector2(-28,-24));
            heading.gameObject.SetActive(true);
            heading.fontSize=PixelTypography.TitleSize(heading,38);heading.fontSizeMin=PixelTypography.TitleSize(heading,28);heading.fontSizeMax=PixelTypography.TitleSize(heading,38);heading.enableAutoSizing=true;
            heading.alignment=TextAlignmentOptions.TopLeft;heading.color=ArenaVisuals.Cyan;heading.raycastTarget=false;
            heading.textWrappingMode=TextWrappingModes.Normal;
            var body=screen.Body;
            Place(body.rectTransform,right,Vector2.zero,Vector2.one,new Vector2(28,184),new Vector2(-28,-132));
            body.gameObject.SetActive(true);
            body.fontSize=PixelTypography.Size(body,30);body.fontSizeMin=PixelTypography.Size(body,26);body.fontSizeMax=PixelTypography.Size(body,30);body.enableAutoSizing=true;
            body.alignment=TextAlignmentOptions.TopLeft;body.textWrappingMode=TextWrappingModes.Normal;body.lineSpacing=6;body.color=Color.white;body.raycastTarget=false;
            StyleButton(screen.PrimaryButton,right,96,164,true);
            StyleButton(screen.SecondaryButton,right,24,80,false);
            var primary=screen.PrimaryButton;var secondary=screen.SecondaryButton;
            primary.navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnDown=secondary,selectOnUp=secondary};
            secondary.navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnDown=primary,selectOnUp=primary};
            var oldAccent=canvas.Find("Accent");if(oldAccent!=null)oldAccent.gameObject.SetActive(false);
            return image;
        }
        static void StyleButton(Button button,Transform parent,float bottom,float top,bool primary)
        {
            Place((RectTransform)button.transform,parent,Vector2.zero,new Vector2(1,0),new Vector2(28,bottom),new Vector2(-28,top));
            button.gameObject.SetActive(true);button.transition=Selectable.Transition.ColorTint;
            var colors=ColorBlock.defaultColorBlock;colors.highlightedColor=new Color(.7f,1,1);colors.selectedColor=new Color(.7f,1,1);button.colors=colors;
            if(button.targetGraphic is Image image){image.sprite=null;image.color=primary?new Color(.04f,.31f,.4f):new Color(.08f,.13f,.2f);}
            var outline=button.GetComponent<Outline>();if(outline==null)outline=button.gameObject.AddComponent<Outline>();
            outline.effectColor=primary?ArenaVisuals.Cyan:new Color(.4f,.55f,.7f);outline.effectDistance=Vector2.one*2;
            foreach(var label in button.GetComponentsInChildren<TMP_Text>(true))
            {
                Place(label.rectTransform,button.transform,Vector2.zero,Vector2.one,new Vector2(12,6),new Vector2(-12,-6));
                label.fontSize=PixelTypography.Size(label,30);label.fontSizeMin=PixelTypography.Size(label,24);label.fontSizeMax=PixelTypography.Size(label,30);label.enableAutoSizing=true;label.alignment=TextAlignmentOptions.Center;
                label.color=Color.white;label.raycastTarget=false;label.textWrappingMode=TextWrappingModes.NoWrap;
            }
        }
    }
}
