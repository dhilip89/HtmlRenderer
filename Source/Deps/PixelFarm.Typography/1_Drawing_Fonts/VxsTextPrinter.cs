﻿//MIT, 2016-2017, WinterDev 
using System;
using System.Collections.Generic;
using System.IO;
using Typography.OpenFont;
using Typography.TextLayout;
using Typography.Rendering;
using PixelFarm.Agg;

namespace PixelFarm.Drawing.Fonts
{

    public class VxsTextPrinter : ITextPrinter
    {
        /// <summary>
        /// target canvas
        /// </summary>
        CanvasPainter canvasPainter;
        IFontLoader _fontLoader;
        RequestFont _font;
        //-----------------------------------------------------------
        GlyphPathBuilder _glyphPathBuilder;
        GlyphLayout _glyphLayout = new GlyphLayout();
        Dictionary<string, GlyphPathBuilder> _cacheGlyphPathBuilders = new Dictionary<string, GlyphPathBuilder>();
        List<GlyphPlan> _outputGlyphPlans = new List<GlyphPlan>();
        //         
        HintedVxsGlyphCollection hintGlyphCollection = new HintedVxsGlyphCollection();
        VertexStorePool _vxsPool = new VertexStorePool();
        GlyphTranslatorToVxs _tovxs = new GlyphTranslatorToVxs();

        public VxsTextPrinter(CanvasPainter canvasPainter, IFontLoader fontLoader)
        {
            this.canvasPainter = canvasPainter;
            this._fontLoader = fontLoader;

            this.ScriptLang = canvasPainter.CurrentFont.GetOpenFontScriptLang();
            ChangeFont(canvasPainter.CurrentFont);
        }
        public void ChangeFont(RequestFont font)
        {
            //1.  resolve actual font file
            this._font = font;
            string resolvedFontFilename = _fontLoader.GetFont(font.Name, font.Style.ConvToInstalledFontStyle()).FontPath;
            if (resolvedFontFilename != _currentFontFilename)
            {
                //switch to another font  
                //store current typeface to cache
                if (_glyphPathBuilder != null && !_cacheGlyphPathBuilders.ContainsKey(resolvedFontFilename))
                {
                    _cacheGlyphPathBuilders[_currentFontFilename] = _glyphPathBuilder;
                }
                //check if we have this in cache ?
                //if we don't have it, this _currentTypeface will set to null                   
                _cacheGlyphPathBuilders.TryGetValue(resolvedFontFilename, out _glyphPathBuilder);
            }
            this._currentFontFilename = resolvedFontFilename;

        }
        public void ChangeFontColor(Color fontColor)
        {
            //change font color

#if DEBUG
            Console.Write("please impl change font color");
#endif
        }
        public HintTechnique HintTechnique
        {
            get;
            set;
        }
        float FontSizeInPoints
        {
            get { return _font.SizeInPoints; }
        }

        public void DrawString(char[] text, int startAt, int len, double x, double y)
        {

            //1. update some props..

            //2. update current type face
            UpdateTypefaceAndGlyphBuilder();
            Typeface typeface = _glyphPathBuilder.Typeface;

            //3. layout glyphs with selected layout technique
            //TODO: review this again, we should use pixel?

            float fontSizePoint = this.FontSizeInPoints;
            _outputGlyphPlans.Clear();
            _glyphLayout.Layout(typeface, fontSizePoint, text, startAt, len, _outputGlyphPlans);

            //4. render each glyph
            float ox = canvasPainter.OriginX;
            float oy = canvasPainter.OriginY;
            int j = _outputGlyphPlans.Count;
            canvasPainter.Clear(PixelFarm.Drawing.Color.White);
            //---------------------------------------------------
            //consider use cached glyph, to increase performance 
            hintGlyphCollection.SetCacheInfo(typeface, fontSizePoint, this.HintTechnique);
            //---------------------------------------------------
            for (int i = 0; i < j; ++i)
            {
                GlyphPlan glyphPlan = _outputGlyphPlans[i];
                //-----------------------------------
                //TODO: review here ***
                //PERFORMANCE revisit here 
                //if we have create a vxs we can cache it for later use?
                //-----------------------------------  
                VertexStore glyphVxs;
                if (!hintGlyphCollection.TryGetCacheGlyph(glyphPlan.glyphIndex, out glyphVxs))
                {
                    //if not found then create new glyph vxs and cache it
                    _glyphPathBuilder.SetHintTechnique(this.HintTechnique);
                    _glyphPathBuilder.BuildFromGlyphIndex(glyphPlan.glyphIndex, fontSizePoint);
                    //-----------------------------------  
                    _tovxs.Reset();
                    _glyphPathBuilder.ReadShapes(_tovxs);

                    //TODO: review here, 
                    //float pxScale = _glyphPathBuilder.GetPixelScale();
                    glyphVxs = new VertexStore();
                    _tovxs.WriteOutput(glyphVxs, _vxsPool);
                    //
                    hintGlyphCollection.RegisterCachedGlyph(glyphPlan.glyphIndex, glyphVxs);
                }
                canvasPainter.SetOrigin((float)(glyphPlan.x + x), (float)(glyphPlan.y + y));
                canvasPainter.Fill(glyphVxs);
            }
            //restore prev origin
            canvasPainter.SetOrigin(ox, oy);
        }

        //-----------------------

        string _currentFontFilename = "";
        public PositionTechnique PositionTechnique
        {
            get { return _glyphLayout.PositionTechnique; }
            set { _glyphLayout.PositionTechnique = value; }
        }

        public bool EnableLigature
        {
            get { return _glyphLayout.EnableLigature; }
            set { this._glyphLayout.EnableLigature = value; }
        }
        public Typography.OpenFont.ScriptLang ScriptLang
        {
            get
            {
                return _glyphLayout.ScriptLang;
            }
            set
            {
                _glyphLayout.ScriptLang = value;
            }
        }


        Typeface UpdateTypefaceAndGlyphBuilder()
        {
            //1. update _glyphPathBuilder for current typeface

            if (_glyphPathBuilder == null)
            {
                //TODO: review here about how to load font file and glyph builder 
                //1. read typeface ...   
                Typeface typeface = null;
                using (FileStream fs = new FileStream(_currentFontFilename, FileMode.Open, FileAccess.Read))
                {
                    var reader = new OpenFontReader();
                    typeface = reader.Read(fs);
                }
                //2. and create
                _glyphPathBuilder = new GlyphPathBuilder(typeface);
                return typeface;
            }
            else
            {
                return _glyphPathBuilder.Typeface;
            }
        }
    }




}