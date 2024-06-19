// Zu prüfende Elemente festlegen
const formInputBoxes = [
    "inputBorderType",
    "inputPadding",
    "inputMargin",
    "inputBorderWidth",
    "inputBorderColor",
    "inputTextColor",
    "inputBgColor",
    "inputBoxShadowHorizontal",
    "inputBorderRadius",
    "inputSelectBackgroundImage",
    "inputBoxBackgroundPositionX",
    "inputBoxBackgroundSizeX",
    "inputSelectBackgroundRepeat",
    "inputSelectFontWeight",
    "inputSelectCustomStyle",
    "inputFontSIze",
    "inputTextDecoration",
    "inputFontFamily",
    "inputLineHeight",
    "inputSelectPositionType",
    "opacityRange",
    "rotationRange",
    "scaleRange",
    "inputDisplayType",
    "inputTextAlign",
    "inputLetterSpacing",
    "inputHeight",
    "inputWidth",
    "inputOverflowX",
    "inputOverflowY",
    "inputSelectFontStyle",
    "inputTextTransform",
    "inputSelectBackgroundBlendMode",
    "inputFilterType"
];

const allForms = [
    "inputBgColor",
    "inputBgColorOpacity",
    "inputBorderColor",
    "inputBorderRadius",
    "inputBorderType",
    "inputBorderWidth",
    "inputBoxBackgroundPositionX",
    "inputBoxBackgroundPositionY",
    "inputBoxBackgroundSizeX",
    "inputBoxBackgroundSizeY",
    "inputBoxShadowBlur",
    "inputBoxShadowHorizontal",
    "inputBoxShadowVertical",
    "inputCustomStyle",
    "inputDisplayFlexAlignSetting",
    "inputDisplayFlexDirectionSetting",
    "inputDisplayFlexJustifySetting",
    "inputDisplayFlexWrapSetting",
    "inputDisplayType",
    "inputFontFamily",
    "inputFontSIze",
    "inputFontSIzeUnit",
    "inputHeight",
    "inputLetterSpacing",
    "inputLetterSpacingUnit",
    "inputLineHeight",
    "inputLineHeightUnit",
    "inputMargin",
    "inputOverflowX",
    "inputOverflowY",
    "inputPadding",
    "inputPositionHorizontalAlign",
    "inputPositionVerticalAlign",
    "inputSelectBackgroundImage",
    "inputSelectBackgroundPositionXUnit",
    "inputSelectBackgroundPositionYUnit",
    "inputSelectBackgroundRepeat",
    "inputSelectBackgroundSizeXUnit",
    "inputSelectBackgroundSizeYUnit",
    "inputSelectBorderRadiusUnit",
    "inputSelectBorderWidthUnit",
    "inputSelectBoxShadowBlurUnit",
    "inputBoxShadowSize",
    "inputSelectBoxShadowSizeUnit",
    "inputSelectCustomStyle",
    "inputSelectFontStyle",
    "inputSelectFontWeight",
    "inputSelectHeightUnit",
    "inputSelectMarginUnit",
    "inputSelectPaddingUnit",
    "inputSelectPositionHorizontalAlign",
    "inputSelectPositionHorizontalAlignUnit",
    "inputSelectPositionType",
    "inputSelectPositionVerticalAlign",
    "inputSelectPositionVerticalAlignUnit",
    "inputSelectShadowHorizontalUnit",
    "inputSelectShadowVerticalUnit",
    "inputSelectWidthUnit",
    "inputShadowColor",
    "inputTextAlign",
    "inputTextColor",
    "inputTextDecoration",
    "inputTextTransform",
    "inputWidth",
    "opacityRange",
    "rotationRange",
    "scaleRange",
    "inputSelectBackgroundBlendMode",
    "inputFilterType",
    "inputFilterValue",
    "inputSelectFilterUnit",
]

// Setzen der Demo Designs
if (localStorage["DESIGNDemo 1"] == undefined) {
    localStorage.setItem("DESIGNDemo 1", '[["inputBgColor","#7d88b0"],["inputBgColorOpacity","1"],["inputBorderColor","#0000ff"],["inputBorderRadius","1"],["inputBorderType",7],["inputBorderWidth","0"],["inputBoxBackgroundPositionX","-56"],["inputBoxBackgroundPositionY","-431"],["inputBoxBackgroundSizeX","158"],["inputBoxBackgroundSizeY",""],["inputBoxShadowBlur","30"],["inputBoxShadowHorizontal","18"],["inputBoxShadowVertical","7"],["inputCustomStyle",""],["inputDisplayFlexAlignSetting",4],["inputDisplayFlexDirectionSetting",0],["inputDisplayFlexJustifySetting",3],["inputDisplayFlexWrapSetting",3],["inputDisplayType",2],["inputFontFamily",5],["inputFontSIze","19"],["inputFontSIzeUnit",5],["inputHeight","88"],["inputLetterSpacing","3"],["inputLetterSpacingUnit",4],["inputLineHeight","24"],["inputLineHeightUnit",4],["inputMargin","2"],["inputOverflowX",4],["inputOverflowY",2],["inputPadding","21"],["inputPositionHorizontalAlign","50"],["inputPositionVerticalAlign","50"],["inputSelectBackgroundImage",2],["inputSelectBackgroundPositionXUnit",3],["inputSelectBackgroundPositionYUnit",4],["inputSelectBackgroundRepeat",1],["inputSelectBackgroundSizeXUnit",7],["inputSelectBackgroundSizeYUnit",13],["inputSelectBorderRadiusUnit",1],["inputSelectBorderWidthUnit",5],["inputSelectBoxShadowBlurUnit",5],["inputBoxShadowSize","0"],["inputSelectBoxShadowSizeUnit",5],["inputSelectCustomStyle",0],["inputSelectFontStyle",1],["inputSelectFontWeight",9],["inputSelectHeightUnit",3],["inputSelectMarginUnit",11],["inputSelectPaddingUnit",5],["inputSelectPositionHorizontalAlign",0],["inputSelectPositionHorizontalAlignUnit",11],["inputSelectPositionType",0],["inputSelectPositionVerticalAlign",0],["inputSelectPositionVerticalAlignUnit",11],["inputSelectShadowHorizontalUnit",4],["inputSelectShadowVerticalUnit",3],["inputSelectWidthUnit",11],["inputShadowColor","#555555"],["inputTextAlign",3],["inputTextColor","#000000"],["inputTextDecoration",7],["inputTextTransform",1],["inputWidth","53"],["opacityRange","81"],["rotationRange","4"],["scaleRange","76"],["inputSelectBackgroundBlendMode",20],["inputFilterType",0],["inputFilterValue","0"],["inputSelectFilterUnit",5]]');
}
if (localStorage["DESIGNDemo 2"] == undefined) {
    localStorage.setItem("DESIGNDemo 2", '[["inputBgColor","#9f86a8"],["inputBgColorOpacity","1"],["inputBorderColor","#000000"],["inputBorderRadius","0"],["inputBorderType",4],["inputBorderWidth","1"],["inputBoxBackgroundPositionX",""],["inputBoxBackgroundPositionY","46"],["inputBoxBackgroundSizeX","234"],["inputBoxBackgroundSizeY",""],["inputBoxShadowBlur","18"],["inputBoxShadowHorizontal","10"],["inputBoxShadowVertical","10"],["inputCustomStyle","0 10% 100% 10%"],["inputDisplayFlexAlignSetting",5],["inputDisplayFlexDirectionSetting",0],["inputDisplayFlexJustifySetting",9],["inputDisplayFlexWrapSetting",2],["inputDisplayType",2],["inputFontFamily",0],["inputFontSIze","16"],["inputFontSIzeUnit",5],["inputHeight","23"],["inputLetterSpacing","0"],["inputLetterSpacingUnit",4],["inputLineHeight",""],["inputLineHeightUnit",7],["inputMargin","21"],["inputOverflowX",0],["inputOverflowY",0],["inputPadding","19"],["inputPositionHorizontalAlign","-6"],["inputPositionVerticalAlign","-5"],["inputSelectBackgroundImage",3],["inputSelectBackgroundPositionXUnit",13],["inputSelectBackgroundPositionYUnit",7],["inputSelectBackgroundRepeat",1],["inputSelectBackgroundSizeXUnit",7],["inputSelectBackgroundSizeYUnit",13],["inputSelectBorderRadiusUnit",7],["inputSelectBorderWidthUnit",3],["inputSelectBoxShadowBlurUnit",5],["inputBoxShadowSize","0"],["inputSelectBoxShadowSizeUnit",5],["inputSelectCustomStyle",3],["inputSelectFontStyle",0],["inputSelectFontWeight",0],["inputSelectHeightUnit",11],["inputSelectMarginUnit",5],["inputSelectPaddingUnit",5],["inputSelectPositionHorizontalAlign",0],["inputSelectPositionHorizontalAlignUnit",11],["inputSelectPositionType",2],["inputSelectPositionVerticalAlign",0],["inputSelectPositionVerticalAlignUnit",11],["inputSelectShadowHorizontalUnit",5],["inputSelectShadowVerticalUnit",5],["inputSelectWidthUnit",11],["inputShadowColor","#6f6f6f"],["inputTextAlign",3],["inputTextColor","#575757"],["inputTextDecoration",0],["inputTextTransform",0],["inputWidth","30"],["opacityRange","100"],["rotationRange","0"],["scaleRange","76"],["inputSelectBackgroundBlendMode",18],["inputFilterType",0],["inputFilterValue","0"],["inputSelectFilterUnit",5]]');
}
if (localStorage["DESIGNDemo 3"] == undefined) {
    localStorage.setItem("DESIGNDemo 3", '[["inputBgColor","#000000"],["inputBgColorOpacity","0.7"],["inputBorderColor","#000000"],["inputBorderRadius","11"],["inputBorderType",7],["inputBorderWidth","4"],["inputBoxBackgroundPositionX",""],["inputBoxBackgroundPositionY","42"],["inputBoxBackgroundSizeX","250"],["inputBoxBackgroundSizeY",""],["inputBoxShadowBlur","32"],["inputBoxShadowHorizontal","50"],["inputBoxShadowVertical","50"],["inputCustomStyle",""],["inputDisplayFlexAlignSetting",1],["inputDisplayFlexDirectionSetting",4],["inputDisplayFlexJustifySetting",10],["inputDisplayFlexWrapSetting",3],["inputDisplayType",2],["inputFontFamily",0],["inputFontSIze","22"],["inputFontSIzeUnit",5],["inputHeight","30"],["inputLetterSpacing","2"],["inputLetterSpacingUnit",4],["inputLineHeight","8"],["inputLineHeightUnit",3],["inputMargin","50"],["inputOverflowX",2],["inputOverflowY",2],["inputPadding","14"],["inputPositionHorizontalAlign","50"],["inputPositionVerticalAlign","50"],["inputSelectBackgroundImage",5],["inputSelectBackgroundPositionXUnit",13],["inputSelectBackgroundPositionYUnit",7],["inputSelectBackgroundRepeat",1],["inputSelectBackgroundSizeXUnit",7],["inputSelectBackgroundSizeYUnit",13],["inputSelectBorderRadiusUnit",7],["inputSelectBorderWidthUnit",5],["inputSelectBoxShadowBlurUnit",5],["inputBoxShadowSize","-17"],["inputSelectBoxShadowSizeUnit",5],["inputSelectCustomStyle",0],["inputSelectFontStyle",0],["inputSelectFontWeight",12],["inputSelectHeightUnit",11],["inputSelectMarginUnit",5],["inputSelectPaddingUnit",5],["inputSelectPositionHorizontalAlign",0],["inputSelectPositionHorizontalAlignUnit",11],["inputSelectPositionType",0],["inputSelectPositionVerticalAlign",0],["inputSelectPositionVerticalAlignUnit",11],["inputSelectShadowHorizontalUnit",5],["inputSelectShadowVerticalUnit",5],["inputSelectWidthUnit",11],["inputShadowColor","#505050"],["inputTextAlign",0],["inputTextColor","#ffffff"],["inputTextDecoration",0],["inputTextTransform",5],["inputWidth","44"],["opacityRange","100"],["rotationRange","0"],["scaleRange","100"],["inputSelectBackgroundBlendMode",11],["inputFilterType",0],["inputFilterValue","0"],["inputSelectFilterUnit",5]]');
}
if (localStorage["DESIGNDemo 4"] == undefined) {
    localStorage.setItem("DESIGNDemo 4", '[["inputBgColor","#000000"],["inputBgColorOpacity","1"],["inputBorderColor","#000046"],["inputBorderRadius","17"],["inputBorderType",7],["inputBorderWidth","4"],["inputBoxBackgroundPositionX",""],["inputBoxBackgroundPositionY",""],["inputBoxBackgroundSizeX","1"],["inputBoxBackgroundSizeY",""],["inputBoxShadowBlur","0"],["inputBoxShadowHorizontal","28"],["inputBoxShadowVertical","-28"],["inputCustomStyle","20px -20px 0 10px rgb(85,85,85)"],["inputDisplayFlexAlignSetting",1],["inputDisplayFlexDirectionSetting",4],["inputDisplayFlexJustifySetting",0],["inputDisplayFlexWrapSetting",2],["inputDisplayType",2],["inputFontFamily",0],["inputFontSIze","24"],["inputFontSIzeUnit",5],["inputHeight","36"],["inputLetterSpacing","4"],["inputLetterSpacingUnit",4],["inputLineHeight","95"],["inputLineHeightUnit",8],["inputMargin","0"],["inputOverflowX",2],["inputOverflowY",2],["inputPadding","30"],["inputPositionHorizontalAlign","35"],["inputPositionVerticalAlign","5"],["inputSelectBackgroundImage",0],["inputSelectBackgroundPositionXUnit",13],["inputSelectBackgroundPositionYUnit",13],["inputSelectBackgroundRepeat",1],["inputSelectBackgroundSizeXUnit",7],["inputSelectBackgroundSizeYUnit",13],["inputSelectBorderRadiusUnit",7],["inputSelectBorderWidthUnit",5],["inputSelectBoxShadowBlurUnit",5],["inputBoxShadowSize","0"],["inputSelectBoxShadowSizeUnit",5],["inputSelectCustomStyle",8],["inputSelectFontStyle",0],["inputSelectFontWeight",10],["inputSelectHeightUnit",11],["inputSelectMarginUnit",5],["inputSelectPaddingUnit",5],["inputSelectPositionHorizontalAlign",0],["inputSelectPositionHorizontalAlignUnit",11],["inputSelectPositionType",1],["inputSelectPositionVerticalAlign",0],["inputSelectPositionVerticalAlignUnit",11],["inputSelectShadowHorizontalUnit",5],["inputSelectShadowVerticalUnit",5],["inputSelectWidthUnit",11],["inputShadowColor","#555555"],["inputTextAlign",3],["inputTextColor","#fbfbfb"],["inputTextDecoration",0],["inputTextTransform",5],["inputWidth","50"],["opacityRange","100"],["rotationRange","97"],["scaleRange","64"],["inputSelectBackgroundBlendMode",20],["inputFilterType",0],["inputFilterValue","0"],["inputSelectFilterUnit",5]]');
}
if (localStorage["DESIGNDemo 5"] == undefined) {
    localStorage.setItem("DESIGNDemo 5", '[["inputBgColor","#f1813d"],["inputBgColorOpacity","0.4"],["inputBorderColor","#0000ff"],["inputBorderRadius","74"],["inputBorderType",7],["inputBorderWidth","0"],["inputBoxBackgroundPositionX","28"],["inputBoxBackgroundPositionY","-24"],["inputBoxBackgroundSizeX","142"],["inputBoxBackgroundSizeY",""],["inputBoxShadowBlur","0"],["inputBoxShadowHorizontal","0"],["inputBoxShadowVertical","0"],["inputCustomStyle",""],["inputDisplayFlexAlignSetting",1],["inputDisplayFlexDirectionSetting",0],["inputDisplayFlexJustifySetting",10],["inputDisplayFlexWrapSetting",2],["inputDisplayType",2],["inputFontFamily",2],["inputFontSIze","15"],["inputFontSIzeUnit",5],["inputHeight","30"],["inputLetterSpacing","0"],["inputLetterSpacingUnit",4],["inputLineHeight",""],["inputLineHeightUnit",7],["inputMargin","0"],["inputOverflowX",2],["inputOverflowY",2],["inputPadding","18"],["inputPositionHorizontalAlign","-15"],["inputPositionVerticalAlign","-15"],["inputSelectBackgroundImage",4],["inputSelectBackgroundPositionXUnit",7],["inputSelectBackgroundPositionYUnit",7],["inputSelectBackgroundRepeat",1],["inputSelectBackgroundSizeXUnit",7],["inputSelectBackgroundSizeYUnit",13],["inputSelectBorderRadiusUnit",5],["inputSelectBorderWidthUnit",5],["inputSelectBoxShadowBlurUnit",5],["inputBoxShadowSize","0"],["inputSelectBoxShadowSizeUnit",5],["inputSelectCustomStyle",0],["inputSelectFontStyle",0],["inputSelectFontWeight",9],["inputSelectHeightUnit",11],["inputSelectMarginUnit",5],["inputSelectPaddingUnit",5],["inputSelectPositionHorizontalAlign",0],["inputSelectPositionHorizontalAlignUnit",11],["inputSelectPositionType",0],["inputSelectPositionVerticalAlign",0],["inputSelectPositionVerticalAlignUnit",11],["inputSelectShadowHorizontalUnit",5],["inputSelectShadowVerticalUnit",5],["inputSelectWidthUnit",11],["inputShadowColor","#000000"],["inputTextAlign",2],["inputTextColor","#000000"],["inputTextDecoration",0],["inputTextTransform",0],["inputWidth","30"],["opacityRange","100"],["rotationRange","0"],["scaleRange","200"],["inputSelectBackgroundBlendMode",1],["inputFilterType",0],["inputFilterValue","0"],["inputSelectFilterUnit",5]]');
}

defaultStyle = '[["inputBgColor","#fadcdc"],["inputBgColorOpacity","1"],["inputBorderColor","#ffff00"],["inputBorderRadius","0"],["inputBorderType",8],["inputBorderWidth","4"],["inputBoxBackgroundPositionX",""],["inputBoxBackgroundPositionY",""],["inputBoxBackgroundSizeX",""],["inputBoxBackgroundSizeY",""],["inputBoxShadowBlur","0"],["inputBoxShadowHorizontal","0"],["inputBoxShadowVertical","0"],["inputCustomStyle",""],["inputDisplayFlexAlignSetting",1],["inputDisplayFlexDirectionSetting",4],["inputDisplayFlexJustifySetting",0],["inputDisplayFlexWrapSetting",2],["inputDisplayType",0],["inputFontFamily",0],["inputFontSIze","16"],["inputFontSIzeUnit",5],["inputHeight","30"],["inputLetterSpacing","0"],["inputLetterSpacingUnit",4],["inputLineHeight",""],["inputLineHeightUnit",7],["inputMargin","0"],["inputOverflowX",0],["inputOverflowY",0],["inputPadding","0"],["inputPositionHorizontalAlign","-15"],["inputPositionVerticalAlign","-15"],["inputSelectBackgroundImage",0],["inputSelectBackgroundPositionXUnit",13],["inputSelectBackgroundPositionYUnit",13],["inputSelectBackgroundRepeat",1],["inputSelectBackgroundSizeXUnit",13],["inputSelectBackgroundSizeYUnit",13],["inputSelectBorderRadiusUnit",5],["inputSelectBorderWidthUnit",5],["inputSelectBoxShadowBlurUnit",5],["inputBoxShadowSize","0"],["inputSelectBoxShadowSizeUnit",5],["inputSelectCustomStyle",0],["inputSelectFontStyle",0],["inputSelectFontWeight",0],["inputSelectHeightUnit",11],["inputSelectMarginUnit",5],["inputSelectPaddingUnit",5],["inputSelectPositionHorizontalAlign",0],["inputSelectPositionHorizontalAlignUnit",11],["inputSelectPositionType",0],["inputSelectPositionVerticalAlign",0],["inputSelectPositionVerticalAlignUnit",11],["inputSelectShadowHorizontalUnit",5],["inputSelectShadowVerticalUnit",5],["inputSelectWidthUnit",11],["inputShadowColor","#000000"],["inputTextAlign",3],["inputTextColor","#000000"],["inputTextDecoration",0],["inputTextTransform",0],["inputWidth","30"],["opacityRange","100"],["rotationRange","0"],["scaleRange","100"],["inputSelectBackgroundBlendMode",20],["inputFilterType",0],["inputFilterValue","0"],["inputSelectFilterUnit",5]]';

// Variabeln für die CSS Textbox
const cssTextAreaTemplate = "#testBoxContainer {\t__TESTBOXCSS__\n}";
var cssTestBoxArray = [];

// IDs der zu manipulierenden Container
const testBoxId = "testContainer";

// Anwenden der Änderungen auf die Zielcontainer
function applyChanges() {
    cssTestBoxArray = []
    for (let index = 0; index < formInputBoxes.length; index++) {
        const element = formInputBoxes[index];

        // Height 
        if (element == "inputHeight") {
            value = document.getElementById("inputHeight").value;
            unit = document.getElementById("inputSelectHeightUnit").value;
            document.getElementById(testBoxId).style.height = value + unit;
            if (value != 0) {
                cssTestBoxArray.push("height: " + value + unit + ";");
            }
            
        // Width 
        } else if (element == "inputWidth") {
            value = document.getElementById("inputWidth").value;
            unit = document.getElementById("inputSelectWidthUnit").value;
            document.getElementById(testBoxId).style.width = value + unit;
            if (value != 0) {
                cssTestBoxArray.push("width: " + value + unit + ";");
            }


        // Padding 
        } else if (element == "inputPadding") {
            value = document.getElementById("inputPadding").value;
            unit = document.getElementById("inputSelectPaddingUnit").value;
            document.getElementById(testBoxId).style.padding = value + unit;
            if (value != 0) {
                cssTestBoxArray.push("padding: " + value + unit + ";");
            }
            
        // Margin 
        } else if (element == "inputMargin") {
            value = document.getElementById("inputMargin").value;
            unit = document.getElementById("inputSelectMarginUnit").value;
            document.getElementById(testBoxId).style.margin = value + unit;
            if (value != 0) {
                cssTestBoxArray.push("margin: " + value + unit + ";");
            }

        // Border Type 
        } else if (element == "inputBorderType") {
            value = document.getElementById("inputBorderType").value;
            borderType = value;
            document.getElementById(testBoxId).style.borderStyle = value;
            if (value != "none") {
                cssTestBoxArray.push("border-style: " + value + ";");
            }

        // Border Width 
        } else if (element == "inputBorderWidth") {
            value = document.getElementById("inputBorderWidth").value;
            unit = document.getElementById("inputSelectBorderWidthUnit").value;
            document.getElementById(testBoxId).style.borderWidth = value + unit;
            if (value != 0 && borderType != "none") {
                cssTestBoxArray.push("border-width: " + value + unit + ";");
            }

        // Border Color
        } else if (element == "inputBorderColor") {
            value = document.getElementById("inputBorderColor").value;
            document.getElementById(testBoxId).style.borderColor = value;
            if (value != "#000000" && borderType != "none")
            cssTestBoxArray.push("border-color: " + value + ";");

        // Textfarben
        } else if (element == "inputTextColor") {
            value = document.getElementById("inputTextColor").value;
            document.getElementById(testBoxId).style.color = value;
            if (value != "#000000") {
                cssTestBoxArray.push("color: " + value + ";");
            }

        // Hintergrundfarbe
        } else if (element == "inputBgColor") {
            value = document.getElementById("inputBgColor").value;
            opacity = document.getElementById("inputBgColorOpacity").value;
            document.getElementById(testBoxId).style.backgroundColor = value;

            if (opacity == 1 && value != "#FFFFFF" && value != "#ffffff") {
                cssTestBoxArray.push("background-color: " + value + ";");
            } else if (opacity != 1) {
                rgbColorArray = hexToRgb(value);

                rgbaValue = "rgba(" + rgbColorArray["r"] + "," + rgbColorArray["g"] + "," + rgbColorArray["b"] + "," + opacity + ")";
                document.getElementById(testBoxId).style.backgroundColor = rgbaValue;
                cssTestBoxArray.push("background-color: " + rgbaValue + ";");
            }

        // Schatten Einstellungen
        } else if (element == "inputBoxShadowHorizontal") {
            value1 = document.getElementById("inputBoxShadowHorizontal").value;
            unit1 = document.getElementById("inputSelectShadowHorizontalUnit").value;
            value2 = document.getElementById("inputBoxShadowVertical").value;
            unit2 = document.getElementById("inputSelectShadowVerticalUnit").value;
            value3 = document.getElementById("inputBoxShadowBlur").value;
            unit3 = document.getElementById("inputSelectBoxShadowBlurUnit").value;
            value4 = document.getElementById("inputBoxShadowSize").value;
            unit4 = document.getElementById("inputSelectBoxShadowSizeUnit").value;
            value5 = document.getElementById("inputShadowColor").value;
            document.getElementById(testBoxId).style.boxShadow = value1 + unit1 + " " + value2 + unit2 + " " + value3 + unit3 + " " + value4 + unit4 + " " + value5;
            if (value1 != 0 || value2 != 0 || value3 != 0 || value4 != 0) {
                cssTestBoxArray.push("box-shadow: " + value1 + unit1 + " " + value2 + unit2 + " " + value3 + unit3 + " " + value4 + unit4 + " " + value5 + ";");
            }


        // Border Radius
        } else if (element == "inputBorderRadius") {
            value = document.getElementById("inputBorderRadius").value;
            unit = document.getElementById("inputSelectBorderRadiusUnit").value;
            document.getElementById(testBoxId).style.borderRadius = value + unit;
            if (value != 0) {
                cssTestBoxArray.push("border-radius: " + value + unit + ";");
            }
        
        // Hintergrundbild
        } else if (element == "inputSelectBackgroundImage") {
            value = document.getElementById("inputSelectBackgroundImage").value;
            if (value != "") {
                showOptions(["BackgroundPositionMenu","BackgroundSizeMenu","BackgroundRepeatMenu", "BackgroundBlendModeMenu"]);
                document.getElementById(testBoxId).style.backgroundImage = "url(" + value + ")";
                cssTestBoxArray.push("background-image: url(" + value + ");");

            } else {
                hideOptions(["BackgroundPositionMenu","BackgroundSizeMenu","BackgroundRepeatMenu", "BackgroundBlendModeMenu"]);
                document.getElementById(testBoxId).style.backgroundImage = "";
            }

        // Hintergrund Effekte
        } else if (element == "inputSelectBackgroundBlendMode") {
            value  = document.getElementById("inputSelectBackgroundBlendMode").value;
            document.getElementById(testBoxId).style.backgroundBlendMode = value;
            if (value != "normal" && value != "unset") {
                cssTestBoxArray.push("background-blend-mode: " + value + ";");
            }

        // Hintergrundposition X-Achse
        } else if (element == "inputBoxBackgroundPositionX") {
            valueX = document.getElementById("inputBoxBackgroundPositionX").value;
            unitX = document.getElementById("inputSelectBackgroundPositionXUnit").value;
            valueY = document.getElementById("inputBoxBackgroundPositionY").value;
            unitY = document.getElementById("inputSelectBackgroundPositionYUnit").value;
            document.getElementById(testBoxId).style.backgroundPositionX = valueX + unitX;
            document.getElementById(testBoxId).style.backgroundPositionY = valueY + unitY;
            if (valueX != 0) {
                cssTestBoxArray.push("background-position-x: " + valueX + unitX + ";");
            }
            if (valueY != 0) {
                cssTestBoxArray.push("background-position-y: " + valueY + unitY + ";");
            }

        // Hintergrundposition Y-Achse
        } else if (element == "inputBoxBackgroundSizeX") {
            valueX = document.getElementById("inputBoxBackgroundSizeX").value;
            unitX = document.getElementById("inputSelectBackgroundSizeXUnit").value;
            valueY = document.getElementById("inputBoxBackgroundSizeY").value;
            unitY = document.getElementById("inputSelectBackgroundSizeYUnit").value;
            document.getElementById(testBoxId).style.backgroundSize = valueX + unitX + " " + valueY + unitY;
            if (unitX != "auto" && unitY != "auto") {
                cssTestBoxArray.push("background-size: " + valueX + unitX + " " + valueY + unitY + ";");
            }

        // Hintergrundwiderholung
        } else if (element == "inputSelectBackgroundRepeat") {
            value = document.getElementById("inputSelectBackgroundRepeat").value;
            document.getElementById(testBoxId).style.backgroundRepeat = value;
            if (value != "repeat") {
                cssTestBoxArray.push("background-repeat: " + value + ";");
            }
        
        // Elementüberlauf Eigenschaften
        } else if (element == "inputOverflowX") {
            value = document.getElementById("inputOverflowX").value;
            document.getElementById(testBoxId).style.overflowX = value;
            if (value != "auto") {
                cssTestBoxArray.push("overflow-x: " + value + ";");
            }
        
        // Elementüberlauf Eigenschaften
        } else if (element == "inputOverflowY") {
            value = document.getElementById("inputOverflowY").value;
            document.getElementById(testBoxId).style.overflowY = value;
            if (value != "auto") {
                cssTestBoxArray.push("overflow-y: " + value + ";");
            }
        
        // Schriftdicke
        } else if (element == "inputSelectFontWeight") {
            value = document.getElementById("inputSelectFontWeight").value;
            document.getElementById(testBoxId).style.fontWeight = value;
            if (value != "normal") {
                cssTestBoxArray.push("font-weight: " + value + ";");
            }
        
        // Schriftstyle
        } else if (element == "inputSelectFontStyle") {
            value = document.getElementById("inputSelectFontStyle").value;
            document.getElementById(testBoxId).style.fontStyle = value;
            if (value != "normal") {
                cssTestBoxArray.push("font-style: " + value + ";");
            }

        // Schriftgrösse
        } else if (element == "inputFontSIze") {
            value = document.getElementById("inputFontSIze").value;
            unit = document.getElementById("inputFontSIzeUnit").value;
            document.getElementById(testBoxId).style.fontSize = value + unit;
            if (value != 16) {
                cssTestBoxArray.push("font-size: " + value + unit + ";");
            }

        // Texttransformation
        } else if (element == "inputTextTransform") {
            value = document.getElementById("inputTextTransform").value;
            document.getElementById(testBoxId).style.textTransform = value;
            if (value != "none") {
                cssTestBoxArray.push("text-transform: " + value + ";");
            }

        // Buchstabenabstand
        } else if (element == "inputLetterSpacing") {
            value = document.getElementById("inputLetterSpacing").value;
            unit = document.getElementById("inputLetterSpacingUnit").value;
            document.getElementById(testBoxId).style.letterSpacing = value + unit;
            if (value != 0) {
                cssTestBoxArray.push("letter-spacing: " + value + unit + ";");
            }

        // Zeilenhöhe
        } else if (element == "inputLineHeight") {
            value = document.getElementById("inputLineHeight").value;
            unit = document.getElementById("inputLineHeightUnit").value;
            document.getElementById(testBoxId).style.lineHeight = value + unit;
            if (value != "" && unit != "") {
                cssTestBoxArray.push("line-height: " + value + unit + ";");
            }
        
        // Textdekoration
        } else if (element == "inputTextDecoration") {
            value = document.getElementById("inputTextDecoration").value;
            document.getElementById(testBoxId).style.textDecoration = value;
            if (value != "none") {
                cssTestBoxArray.push("text-decoration: " + value + ";");
            }
        
        // Schriftart
        } else if (element == "inputFontFamily") {
            value = document.getElementById("inputFontFamily").value;
            document.getElementById(testBoxId).style.fontFamily = value;
            cssTestBoxArray.push("font-family: " + value + ";");
        
        // Transparenz
        } else if (element == "opacityRange") {
            value = document.getElementById("opacityRange").value / 100;
            document.getElementById(testBoxId).style.opacity = value;
            document.getElementById("opacityRangeValue").innerHTML = value;
            if (value != 1) {
                cssTestBoxArray.push("opacity: " + value + ";");
            }
        
        // Rotation
        } else if (element == "rotationRange") {
            rotation = document.getElementById("rotationRange").value / 100;
            scale = (document.getElementById("scaleRange").value) / 100;
            document.getElementById(testBoxId).style.transform = "rotate(" + rotation + "turn) scale(" + scale + ")";
            document.getElementById("rotationRangeValue").innerHTML = rotation + " turn";        
            document.getElementById("scaleRangeValue").innerHTML = scale;
            if (value != 1) {
                cssTestBoxArray.push("scale: " + value + ";");
            }
        
        // Textausrichtung
        } else if (element == "inputTextAlign") {
            value = document.getElementById("inputTextAlign").value;
            document.getElementById(testBoxId).style.textAlign = value;
            if (value != "left" && value != "start") {
                cssTestBoxArray.push("text-align: " + value + ";");
            }
        
        // Position Einstellungen
        } else if (element == "inputSelectPositionType") {
            positionType = document.getElementById("inputSelectPositionType").value;
            eval("document.getElementById('" + testBoxId + "').style.position = '" + positionType + "'");
            
            if (positionType != "unset") {
                cssTestBoxArray.push("position: " + positionType + ";");
                showOptions(['HorizontalAlign', 'VerticalAlign']);

                horizontalOrientation = document.getElementById("inputSelectPositionHorizontalAlign").value;
                horizontalPosition = document.getElementById("inputPositionHorizontalAlign").value;
                horizontalPositionUnit = document.getElementById("inputSelectPositionHorizontalAlignUnit").value;
                verticalOrientation = document.getElementById("inputSelectPositionVerticalAlign").value;
                verticalPosition = document.getElementById("inputPositionVerticalAlign").value;
                verticalPositionUnit = document.getElementById("inputSelectPositionVerticalAlignUnit").value;

                if (horizontalOrientation == "left") {
                    document.getElementById(testBoxId).style.right = null;
                    document.getElementById(testBoxId).style.left = horizontalPosition + horizontalPositionUnit;
                    cssTestBoxArray.push("left: " + horizontalPosition + horizontalPositionUnit + ";");

                } else if (horizontalOrientation == "right") {
                    document.getElementById(testBoxId).style.left = null;
                    document.getElementById(testBoxId).style.right = horizontalPosition + horizontalPositionUnit;
                    cssTestBoxArray.push("right: " + horizontalPosition + horizontalPositionUnit + ";");
                }
                
                if (verticalOrientation == "top") {
                    document.getElementById(testBoxId).style.bottom = null;
                    document.getElementById(testBoxId).style.top = verticalPosition + verticalPositionUnit;
                    cssTestBoxArray.push("top: " + verticalPosition + verticalPositionUnit + ";");

                } else if (verticalOrientation == "bottom") {
                    document.getElementById(testBoxId).style.top = null;
                    document.getElementById(testBoxId).style.bottom = verticalPosition + verticalPositionUnit;
                    cssTestBoxArray.push("bottom: " + verticalPosition + verticalPositionUnit + ";");

                }
            } else {
                hideOptions(['HorizontalAlign', 'VerticalAlign']);
            }

        // Effektfilter
        } else if (element == "inputFilterType") {
            filterType = document.getElementById("inputFilterType").value
            filterValue = document.getElementById("inputFilterValue").value;
            filterUnit = document.getElementById("inputSelectFilterUnit").value;
            
            if (filterType != "none") {
                if (filterType == "blur") {
                    showOptions(['inputFilterValuesWrapper','inputFilterValue', 'inputSelectFilterUnit']);
                    document.getElementById(testBoxId).style.filter = filterType + "(" + filterValue + filterUnit + ")";
                    cssTestBoxArray.push("filter: " + filterType + "("+ filterValue + filterUnit + ")");
                } else if (filterType == "hue-rotate") {
                    showOptions(['inputFilterValuesWrapper','inputFilterValue']);
                    hideOptions(['inputSelectFilterUnit']);
                    document.getElementById(testBoxId).style.filter = filterType + "(" + filterValue + "turn)";
                    cssTestBoxArray.push("filter: " + filterType + "("+ filterValue + "turn)");
                } else {
                    showOptions(['inputFilterValuesWrapper','inputFilterValue']);
                    hideOptions(['inputSelectFilterUnit']);
                    document.getElementById(testBoxId).style.filter = filterType + "(" + filterValue + ")";
                    cssTestBoxArray.push("filter: " + filterType + "("+ filterValue + ")");
                }
            } else {
                hideOptions(['inputFilterValuesWrapper','inputFilterValue', 'inputSelectFilterUnit']);
                document.getElementById(testBoxId).style.filter = "none";
            }



        // Display Einstellungen
        } else if (element == "inputDisplayType") {
            displayType = document.getElementById(element).value;
            textBox = document.getElementById(testBoxId);

            textBox.style.display = displayType;
            if (displayType != "block") {
                cssTestBoxArray.push("display: " + displayType + ";");
            }


            if (displayType == "flex" || displayType == "inline-flex") {
                showOptions(["FlexDirectionSetting","FlexWrapSetting","JustifyContentSetting","AlignContentSetting"]);
                flexDirection = document.getElementById("inputDisplayFlexDirectionSetting").value;
                flexWrap = document.getElementById("inputDisplayFlexWrapSetting").value;
                flexJustify = document.getElementById("inputDisplayFlexJustifySetting").value;
                flexAlign = document.getElementById("inputDisplayFlexAlignSetting").value;

                textBox.style.flexDirection = flexDirection;
                textBox.style.flexWrap = flexWrap;
                textBox.style.justifyContent = flexJustify;
                textBox.style.alignContent = flexAlign;

                cssTestBoxArray.push("flex-direction: " + flexDirection + ";");
                cssTestBoxArray.push("flex-wrap: " + flexWrap + ";");
                cssTestBoxArray.push("justify-content: " + flexJustify + ";");
                cssTestBoxArray.push("align-content: " + flexAlign + ";");
            } else {
                hideOptions(["FlexDirectionSetting","FlexWrapSetting","JustifyContentSetting","AlignContentSetting"]);
                textBox.style.flexDirection = "";
                textBox.style.flexWrap = "";
                textBox.style.justifyContent = "";
                textBox.style.alignContent = "";
            }
        
        // Custom Einstellungen
        } else if (element == "inputSelectCustomStyle") {
            styleClass = document.getElementById("inputSelectCustomStyle").value;
            if (styleClass != "") {
                style = document.getElementById("inputCustomStyle").value;
                eval("document.getElementById('" + testBoxId + "').style." + styleClass + " = '" + style + "'");

                if (styleClass == "margin") {
                    cssTestBoxArray.push("margin: " + style + ";");
                } else if (styleClass == "padding") {
                    cssTestBoxArray.push("padding: " + style + ";");
                } else if (styleClass == "borderRadius") {
                    cssTestBoxArray.push("border-radius: " + style + ";");
                } else if (styleClass == "borderLeft") {
                    cssTestBoxArray.push("border-left: " + style + ";");
                } else if (styleClass == "borderRight") {
                    cssTestBoxArray.push("border-right: " + style + ";");
                } else if (styleClass == "borderTop") {
                    cssTestBoxArray.push("border-top: " + style + ";");
                } else if (styleClass == "borderBottom") {
                    cssTestBoxArray.push("border-bottom: " + style + ";");
                } else if (styleClass == "boxShadow") {
                    cssTestBoxArray.push("box-shadow: " + style + ";");
                } else if (styleClass == "fontSize") {
                    cssTestBoxArray.push("font-size: " + style + ";");
                }
            }
        }
    }
    adjustImageSize()
    fillCssTextBox()
}

// Funktion um die Auswahl zweier Dropdowns anzugleichen
function equalOptions(oppositeSelection, valuesToReset, index) {
    var element = document.getElementById(oppositeSelection);
    var elements = document.getElementById(oppositeSelection).options;

    for (let index = 0; index < valuesToReset.length; index++) {
        var key = valuesToReset[index];
        document.getElementById(key).value = "";
    }

    for(var i = 0; i < elements.length; i++){
        elements[i].selected = false;
        if (i == index) {
            elements[i].selected = 'selected';
        }
    }

    applyChanges();
}

// Funktion, um Werte aus Input-Boxen zu entfernen
function removeValuesOf(idArray) {
    for (let index = 0; index < idArray.length; index++) {
        var element = document.getElementById(idArray[index]);
        element.value = "";
    }
    applyChanges();
}

// Funktion, die Einstellungssegmente auf und zuzuklappen
// Benötigt definierte Klassen "fadeIn" und "fadeOut" sowie entsprechende CSS Animationen
function toggleSettings(id) {
    elem = document.getElementById(id);
    sibling = elem.previousElementSibling;

    if (elem.classList.length == 0) {
        elem.classList.add("fadeIn");
        sibling.classList.replace("closed", "open");
    } else if (elem.classList[0] == "fadeOut") {
        sibling.classList.replace("closed", "open");
        elem.classList.replace("fadeOut", "fadeIn");
    } else {
        elem.classList.replace("fadeIn", "fadeOut");
        sibling.classList.replace("open", "closed");
    }
}

// Funktion, um Einstellungen auszublenden
function hideOptions(idArray) {
    for (let index = 0; index < idArray.length; index++) {
        const element = document.getElementById(idArray[index]);
        if (!element.classList.contains("hidden")) {
            element.classList.add("hidden");
        }
    }
}

// Funktion, um Einstellungen einzublenden
function showOptions(idArray) {
    for (let index = 0; index < idArray.length; index++) {
        const element = document.getElementById(idArray[index]);
        if (element.classList.contains("hidden")) {
            element.classList.remove("hidden");
        }
    }
}

// Funktion, um die Grösse eines Bildes auf 30% des Elternelements einzustellen
function adjustImageSize() {
    element = document.getElementById("testContainerImage");
    parent = element.parentElement;
    parentWidth = parent.offsetWidth;

    element.style.width = parentWidth * 0.3 + "px";
}

// Funktion, um die Namen der gespeicherten Designs in das vorgesehene Dropdown einzufügen
function getSavedDesigns() {
    selection = document.getElementById("loadFormSelect");
    selection.innerHTML="";
    keys = [];
    for (var key in localStorage){
        if (key.startsWith("DESIGN")) {
            keys.push(key);
        }
    }
    sorted = keys.sort();

    for (let index = 0; index < sorted.length; index++) {
        key = sorted[index];
        option = document.createElement('option');
        option.value = key.replace("DESIGN", "");
        option.innerHTML = key.replace("DESIGN", "");
        selection.appendChild(option);  
    };
}

// Funktion, um gespeicherte Designs zu laden
function loadDesign(def=false, customDesign="") {
    if (def == false) {
        designName = document.getElementById("loadFormSelect").value;
    } else {
        designName = "default";
    }
    if (designName != "") {
        if (def == false) {
            designArray = JSON.parse(localStorage["DESIGN" + designName]);
        } else if (customDesign != "") {
            designArray = JSON.parse(customDesign);
        } else {
            designArray = [];
        }
        for (let index = 0; index < designArray.length; index++) {
            const elementName = designArray[index][0];
            const element = document.getElementById(elementName);
            const value = designArray[index][1]

            if (element.tagName == "INPUT") {
                element.value = value;
            } else if (element.tagName == "SELECT") {
                element.selectedIndex = value;
            }
        }
    }

    hideElement("loadForm");
    applyChanges()
}

// Funktion, um Designs zu speichern
function saveDesign() {
    designName = document.getElementById("saveAsName").value;
    if (designName != "") {
        results = []
        for (let index = 0; index < allForms.length; index++) {
            const element = document.getElementById(allForms[index]);
            if (element.tagName == "SELECT") {
                currentValue = element.selectedIndex;
            } else if (element.tagName == "INPUT") {
                currentValue = element.value;
            }
            results[index] = [allForms[index], currentValue]
        }
        localStorage.setItem("DESIGN"+designName, JSON.stringify(results));
        hideElement("saveForm");
        getSavedDesigns()
    }
}

// Funktion, um gespeicherte Designs zu löschen
function deleteDesign() {
    designName = document.getElementById("loadFormSelect").value;
    if (confirm('Bist du sicher, dass du das Design '+designName+' löschen möchtest?')) {
        localStorage.removeItem("DESIGN"+designName);
        hideElement("loadForm");
        getSavedDesigns()
      }
}

// Funktion, um Elemente zu verstecken
function hideElement(id) {
    element = document.getElementById(id);
    element.classList.add("hidden");
}

// Funktion, um versteckte Elemente anzuzeigen
function showElement(id) {
    element = document.getElementById(id);
    element.classList.remove("hidden")
    // element.style.display = "flex !important";
}

// Funktion, um den CSS Code in das Textfeld einzufügen
function fillCssTextBox() {
    element = document.getElementById("cssTextArea");
    cssTestBoxArray = cssTestBoxArray.sort()
    output = ""
    for (let index = 0; index < cssTestBoxArray.length; index++) {
        const current = cssTestBoxArray[index];
        output += "\n\t" + current;
    }
    newText = cssTextAreaTemplate.replace("__TESTBOXCSS__", output);
    element.value = newText;    
}

// Funktion, um Hexadezimalzahlen in RGB umzuwandeln
function hexToRgb(hex) {
    var result = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(hex);
    return result ? {
      r: parseInt(result[1], 16),
      g: parseInt(result[2], 16),
      b: parseInt(result[3], 16)
    } : null;
  }

// Funktion, um einen Tipp anzuzeigen
async function showHint(message) {
    const element = document.getElementById("infoBox");
    element.innerHTML = message;
    document.addEventListener('mousemove', getMousePosition, false);
    showElement('infoBox');
}

// Funktion, um den Tipp zu verbergen
function hideHint() {
    document.removeEventListener('mousemove', getMousePosition, false);
    hideElement("infoBox");
}

// Funktion, um den Tipp der Position der Maus anzupassen
function getMousePosition( event ) {
    const element = document.getElementById("infoBox");
    var mousePositionX = event.clientX + 20;
    var mousePositionY = event.clientY - 11;

    element.style.left = mousePositionX + "px";
    element.style.top = mousePositionY + "px";
}

function resetDesign() {
    loadDesign(true, defaultStyle);
}

// Random 
imgContainer = document.getElementById("testContainerImage");
possibleImages = [
    "bilder/bild.png",
    "bilder/bild2.png",
    "bilder/bild3.png",
    "bilder/bild4.png",
    "bilder/bild5.png",
    "bilder/headerbild.png",
    "bilder/logo.png"
];
backgroundImage = possibleImages[Math.floor(Math.random()*possibleImages.length)];
imgContainer.src = backgroundImage;

adjustImageSize();
getSavedDesigns();
applyChanges(); 