{"keys_left": 0, "image": "7635c068-137f-4164-9d63-685702dde5f6.png", "keys_consumed_last_minute": 5, "simon-says": "200", "jspayload": "if ( 'countDownTimer' in globalData )
{
 clearInterval( globalData.countDownTimer );
}

globalData.countDownEnd = new Date("2019-03-06T17:00:00Z");

function startTimer()
{ 
 if( fakeUpdate )
 {
 clearInterval( fakeUpdate );
 fakeUpdate = null;
 }
 document.body.style.backgroundColor = "#000";
 document.body.style.textAlign = "center";

 document.body.style.fontFamily = "Tahoma";
 
 var DOMimage = document.getElementById( "simon" );
 DOMimage.style.display = "none";
 DOMimage.removeEventListener( 'load', recalcKeys );
 DOMimage.src = "7635c068-137f-4164-9d63-685702dde5f6.png";
 
 var DOMcounter = document.getElementById( "keys_left" );
 DOMcounter.style.display = "block";
 DOMcounter.style.color = "#fff";
 DOMcounter.style.position = "absolute";
 DOMcounter.style.top = "50%";
 DOMcounter.style.transform = "translateY(-50%)";
 DOMcounter.style.textAlign = "center";
 DOMcounter.style.transform = "translateX(-50%)";
 DOMcounter.style.left = "50%";
 
 timerTick();
 globalData.countDownTimer = setInterval( timerTick, 500 );
}

function timerTick()
{
 var DOMcounter = document.getElementById( "keys_left" );
 var currentTime = new Date();

 var timeLeft = Math.floor((globalData.countDownEnd - currentTime) / 1000);
 if( timeLeft <= 0 )
 {
 loadSimon();
 return;
 }
 DOMcounter.innerHTML = secondsToString( timeLeft ); 
}

function killAllChildren( DOMparent )
{
 while( DOMparent.firstChild )
 {
 DOMparent.removeChild( DOMparent.firstChild );
 }
}

function secondsToString(seconds)
{
 seconds = seconds - (3600) * 2;
 numdays = 0;
 var numhours = Math.floor(((seconds % 31536000)) / 3600);
 var numminutes = Math.floor((((seconds % 31536000) % 86400) % 3600) / 60);
 var numseconds = (((seconds % 31536000) % 86400) % 3600) % 60;
 if( numdays > 0 ) {
 return numdays + "d "+ numhours + "h "+ numminutes + "m "+ numseconds + "s";
 } else {
 return numhours.toString().startPad(3, "0") + "h "+ numminutes.toString().startPad(3, "0") + "m "+ numseconds.toString().startPad(3, "0") + "s";
 }

}

if ( 'countDownTimer' in globalData )
{
 clearInterval( globalData.countDownTimer );
}

globalData.countDownEnd = new Date("2019-03-06T19:00:00Z");

function startTimer()
{ 
 if( fakeUpdate )
 {
 clearInterval( fakeUpdate );
 fakeUpdate = null;
 }
 document.body.style.backgroundColor = "#000";
 document.body.style.textAlign = "center";

 document.body.style.fontFamily = "Tahoma";
 
 var DOMimage = document.getElementById( "simon" );
 DOMimage.style.display = "none";
 DOMimage.removeEventListener( 'load', recalcKeys );
 DOMimage.src = "7635c068-137f-4164-9d63-685702dde5f6.png";
 
 var DOMcounter = document.getElementById( "keys_left" );
 DOMcounter.style.display = "block";
 DOMcounter.style.color = "#fff";
 DOMcounter.style.position = "absolute";
 DOMcounter.style.top = "50%";
 DOMcounter.style.transform = "translateY(-50%)";
 DOMcounter.style.textAlign = "center";
 DOMcounter.style.transform = "translateX(-50%)";
 DOMcounter.style.left = "50%";
 
 timerTick();
 globalData.countDownTimer = setInterval( timerTick, 500 );
}

function timerTick()
{
 var DOMcounter = document.getElementById( "keys_left" );
 var currentTime = new Date();

 var timeLeft = Math.floor((globalData.countDownEnd - currentTime) / 1000);
 if( timeLeft <= 0 )
 {
 loadSimon();
 return;
 }
 DOMcounter.innerHTML = secondsToString( timeLeft ); 
}

function killAllChildren( DOMparent )
{
 console.log( typeof( DOMparent ) );

 while( DOMparent.firstChild )
 {
 DOMparent.removeChild( DOMparent.firstChild );
 }
}

function secondsToString(seconds)
{
 numdays = 0;
 var numhours = Math.floor(((seconds % 31536000)) / 3600);
 var numminutes = Math.floor((((seconds % 31536000) % 86400) % 3600) / 60);
 var numseconds = (((seconds % 31536000) % 86400) % 3600) % 60;

 var returnstring = "";
 if( numhours < 10 )
 {
 returnstring += "0";
 }
 returnstring += numhours + "h ";

 if( numminutes < 10 )
 {
 returnstring += "0";
 }
 returnstring += numminutes + "m ";

 if( numseconds < 10 )
 {
 returnstring += "0";
 }
 returnstring += numseconds + "s"
 return returnstring;
}

startTimer();"}
