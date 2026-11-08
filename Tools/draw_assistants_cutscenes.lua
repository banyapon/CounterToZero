local root='D:/GitHub/Unity/CounterToZero/'
local pc=app.pixelColor
local function C(r,g,b,a)return pc.rgba(r,g,b,a or 255)end
for variant=1,2 do
 local s=app.open(root..'Assets/GameAssets/Sprites/man/1 walk.png');local im=Image(s.cels[1].image)
 for y=0,im.height-1 do for x=0,im.width-1 do local p=im:getPixel(x,y);local r,g,b=pc.rgbaR(p),pc.rgbaG(p),pc.rgbaB(p)
 if pc.rgbaA(p)>0 and ((r==67 and g==43) or (r==112 and g==72)) then im:drawPixel(x,y,variant==1 and C(44,167,91) or C(255,207,55))end end end
 for row=0,3 do for col=0,2 do local x,y=col*16,row*16
 if variant==1 then
  for dx=3,12 do for dy=2,5 do im:drawPixel(x+dx,y+dy,C(39,139,77))end end
  for dx=4,11 do im:drawPixel(x+dx,y+5,C(85,214,123))end
  for dy=5,11 do im:drawPixel(x+3,y+dy,C(31,112,69));im:drawPixel(x+12,y+dy,C(31,112,69))end
 else for dx=4,11 do im:drawPixel(x+dx,y+2,C(255,225,83))end;im:drawPixel(x+8,y,C(255,225,83));im:drawPixel(x+9,y+1,C(255,225,83))end
 end end
 s.cels[1].image=im;local name=variant==1 and 'assistant_green' or 'assistant_gold';s:saveAs(root..'ArtSource/'..name..'.aseprite');s:saveCopyAs(root..'Assets/GameAssets/Sprites/man/'..name..'.png');s:close()
end
local ink=C(13,20,39);local paper=C(220,232,222);local blue=C(74,182,208);local pink=C(238,78,106)
for panel=1,3 do
 local s=Sprite(320,240,ColorMode.RGB);local im=Image(320,240,ColorMode.RGB)
 local function rect(x,y,w,h,c)for yy=y,y+h-1 do for xx=x,x+w-1 do if xx>=0 and xx<320 and yy>=0 and yy<240 then im:drawPixel(xx,yy,c)end end end end
 local function line(x,y,x2,y2,c)local n=math.max(math.abs(x2-x),math.abs(y2-y));for i=0,n do rect(math.floor(x+(x2-x)*i/math.max(1,n)),math.floor(y+(y2-y)*i/math.max(1,n)),1,1,c)end end
 rect(0,0,320,240,ink);rect(6,6,308,228,paper)
 -- hand inked circuit perspective, speed hatching and comic gutters
 for i=0,12 do line(160,105,i*27,234,blue)end
 for i=1,8 do line(8,110+i*i*2,312,110+i*i*2,blue)end
 local function person(x,y,hair,face)
  -- Tapered shoulders, collar, bent arms and shaded anime face.
  for yy=0,49 do local w=yy<12 and 14+math.floor(yy*.7) or 22-math.floor((yy-12)*.12);rect(x-w,y+38+yy,w*2,1,ink);rect(x-w+3,y+38+yy,w*2-6,1,C(250,249,228))end
  rect(x-5,y+32,10,13,C(237,186,145));line(x-10,y+42,x,y+51,ink);line(x,y+51,x+10,y+42,ink)
  line(x-16,y+53,x-12,y+85,ink);line(x+16,y+53,x+12,y+85,ink);line(x-12,y+68,x+4,y+80,blue)
  for yy=0,39 do local w=yy<27 and 15 or 15-math.floor((yy-27)*.65);rect(x-w,y+yy,w*2,1,ink);rect(x-w+2,y+yy,w*2-4,1,C(237,186,145))end
  rect(x-15,y-5,30,17,hair)
  if pc.rgbaG(hair)>pc.rgbaR(hair) then
   for yy=0,43 do rect(x-18,y+yy,5,1,hair);rect(x+13,y+yy,5,1,hair)end
   for i=0,5 do line(x-14+i*5,y+5,x-13+i*5,y+14,ink)end
  else
   for i=0,5 do for j=0,4 do line(x-16+i*6+j,y-5,x-11+i*5,y-14+(i%2)*6,hair)end;line(x-15+i*6,y+6,x-12+i*5,y+15,ink)end
  end
  line(x-11,y+19,x-3,y+17,ink);line(x+3,y+17,x+11,y+19,ink)
  rect(x-10,y+21,7,5,C(250,249,228));rect(x+3,y+21,7,5,C(250,249,228));rect(x-7,y+21,3,5,C(108,65,38));rect(x+4,y+21,3,5,C(108,65,38));rect(x-7,y+21,1,2,ink);rect(x+4,y+21,1,2,ink)
  line(x-11,y+20,x-3,y+20,ink);line(x+3,y+20,x+11,y+20,ink);line(x,y+25,x-2,y+29,C(158,104,86));line(x-3,y+33,x+4,y+32,ink)
  for i=0,3 do line(x-16,y+49+i*5,x-8,y+58+i*5,ink)end
 end
 if panel==1 then
  person(94,97,C(91,58,41));rect(195,48,81,106,ink);rect(201,54,69,94,blue);rect(215,76,40,44,ink)
  for i=0,8 do rect(147+i*8,120+math.floor(math.sin(i)*16),3+i%3,3,blue)end
  for i=0,17 do line(13+i*5,12,20+i*5,60,ink)end
 elseif panel==2 then
  person(91,103,C(48,174,96));person(230,103,C(250,210,65));person(160,121,C(91,58,41))
  rect(124,27,72,50,ink);for i=0,5 do line(126+i*12,25,112+i*17,12,pink)end;rect(136,42,15,5,pink);rect(169,42,15,5,pink)
  for i=0,16 do line(10,20+i*6,65,60+i*4,ink);line(310,20+i*6,255,60+i*4,ink)end
 else
  rect(105,30,110,75,ink);rect(112,37,96,61,blue);line(115,90,202,41,paper);line(115,41,202,90,paper)
  for i=0,30 do local x=30+(i*37)%260;local y=20+(i*19)%95;rect(x,y,3+i%4,3,blue)end
  person(98,136,C(48,174,96));person(223,136,C(250,210,65));person(160,122,C(91,58,41))
 end
 rect(6,6,308,3,ink);rect(6,231,308,3,ink);rect(6,6,3,228,ink);rect(311,6,3,228,ink)
 s.cels[1].image=im;s:saveAs(root..'ArtSource/cutscene_'..panel..'.aseprite');s:saveCopyAs(root..'Assets/GameAssets/Sprites/cutscene_'..panel..'.png');s:close()
end
