-- Layered Aseprite comic panels, using the supplied cast illustration as reference art.
local root='D:/GitHub/Unity/CounterToZero/'
local pc=app.pixelColor
local function c(r,g,b)return pc.rgba(r,g,b,255)end
local ink=c(8,15,31);local cyan=c(55,223,221);local red=c(255,40,94);local white=c(235,249,235)
local ref=app.open(root..'Assets/GameAssets/UI/title.png')
local cast=Image(ref.cels[1].image);ref:close()
for panel=1,3 do
 local s=Sprite(480,360,ColorMode.RGB);s.layers[1].name='Circuit environment'
 local im=Image(480,360,ColorMode.RGB)
 local function rect(x,y,w,h,color)for yy=math.max(0,y),math.min(359,y+h-1)do for xx=math.max(0,x),math.min(479,x+w-1)do im:drawPixel(xx,yy,color)end end end
 local function line(x,y,u,v,color,width)local n=math.max(math.abs(u-x),math.abs(v-y));for i=0,n do rect(math.floor(x+(u-x)*i/math.max(1,n)),math.floor(y+(v-y)*i/math.max(1,n)),width or 1,width or 1,color)end end
 local function diamond(x,y,w,h,color)for yy=-h,h do local xx=math.floor(w*(1-math.abs(yy)/h));rect(x-xx,y+yy,xx*2+1,1,color)end end
 local function ring(x,y,r,color)for a=0,359 do local t=a*math.pi/180;rect(math.floor(x+math.cos(t)*r),math.floor(y+math.sin(t)*r),2,2,color)end end
 rect(0,0,480,360,ink)
 -- A pixel-stepped horizon and circuit floor, different light for each story beat.
 for y=0,359 do rect(0,y,480,1,panel==3 and c(12+math.floor(y/12),32+math.floor(y/7),47+math.floor(y/8)) or c(8+math.floor(y/30),15+math.floor(y/18),31+math.floor(y/12)))end
 for i=0,17 do local x=i*29;local h=20+(i*31)%100;rect(x,160-h,22,h,c(16,32,53));rect(x+2,162-h,2,h-5,c(28,73,91));for j=1,4 do rect(x+7,158-h+j*13,9,3,panel==3 and cyan or c(104,58,130))end end
 for x=-480,960,60 do line(240,145,x,359,c(24,79,99))end
 for y=180,359,22 do line(8,y,472,y,c(24,79,99))end
 for i=1,12 do local x=(i*83)%470;local y=45+(i*29)%140;line(x,y,x+20,y,c(31,108,128));line(x+20,y,x+30,y-10,c(31,108,128));rect(x+29,y-11,3,3,cyan)end
 s.cels[1].image=im
 local l=s:newLayer();l.name=panel==3 and 'Liberated core and shattered virus' or 'Omega virus and transfer gate';im=Image(480,360,ColorMode.RGB)
 if panel==1 then
  -- A transfer gate surrounds the team; the hostile core watches from a monitor.
  for r=125,133,2 do ring(220,189,r,cyan)end
  rect(328,37,126,116,c(36,75,100));rect(332,41,118,108,ink)
  diamond(391,91,44,42,red);diamond(391,91,34,33,c(54,7,32));rect(363,87,57,13,red);rect(372,91,9,5,white)
  for i=0,8 do rect(24+i*12,48,8,3,cyan);rect(26+i*12,56,4,2,c(39,91,119))end
 elseif panel==2 then
  -- Preserve the in-game boss's diamond shell, dark core and single eye.
  for r=74,90,8 do ring(240,102,r,c(94,25,75))end
  diamond(240,105,81,78,red);diamond(240,105,64,62,c(59,6,31));diamond(240,95,49,42,c(91,12,44))
  rect(180,96,121,25,red);rect(197,102,22,13,c(35,3,20));rect(201,103,6,4,white)
  for i=1,6 do local x=50+i*55;line(x,22,x+(x-240)/4,4,c(160,30,84),2)end
  for i=0,5 do ring(154+i*34,187+math.floor(math.sin(i)*8),5,red)end
  line(10,80,99,140,red);line(470,80,381,140,red)
 else
  for r=50,64,4 do ring(240,93,r,c(90,230,203))end
  diamond(240,94,43,42,cyan);diamond(240,94,33,32,c(27,72,83))
  line(222,94,235,106,white,3);line(235,106,258,77,white,3)
  for i=0,21 do local x=70+(i*47)%350;local y=28+(i*23)%145;local color=i%3==0 and red or cyan;rect(x,y,3+i%5,3+i%3,color);line(x,y-4,x+2,y-9,c(42,94,107))end
  line(170,60,185,81,red,3);line(294,109,309,129,red,3)
 end
 s:newCel(l,1,im,Point(0,0))
 l=s:newLayer();l.name='Title cast - white suits, green brown and gold hair'
 im=Image(480,360,ColorMode.RGB)
 -- Keep exact pixel silhouettes and facial details of the user-provided illustration.
 im:drawImage(cast,Point(0,90));s:newCel(l,1,im,Point(0,0))
 l=s:newLayer();l.name='Comic border and foreground sparks';im=Image(480,360,ColorMode.RGB)
 rect(0,0,480,5,ink);rect(0,355,480,5,ink);rect(0,0,5,360,ink);rect(475,0,5,360,ink)
 rect(9,9,70,3,panel==3 and cyan or red);rect(9,15,36,2,white)
 for i=1,18 do local x=(i*37)%465+7;local y=210+(i*29)%130;if x<90 or x>420 then rect(x,y,2,5,panel==2 and red or cyan)end end
 s:newCel(l,1,im,Point(0,0))
 s:saveAs(root..'ArtSource/cutscene_'..panel..'.aseprite');s:saveCopyAs(root..'Assets/GameAssets/Sprites/cutscene_'..panel..'.png');s:close()
end
