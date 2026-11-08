local root='D:/GitHub/Unity/CounterToZero/'
local pc=app.pixelColor
local function C(r,g,b,a)return pc.rgba(r,g,b,a or 255)end
local ink=C(19,20,33);local hair=C(91,55,36);local gold=C(145,94,53);local skin=C(244,191,145);local shade=C(188,123,93);local white=C(243,247,233);local grey=C(154,181,178);local dark=C(51,68,78);local cyan=C(72,234,255)
local s=Sprite(192,128,ColorMode.RGB);s.layers[1].name='Brown-haired hero / charge rifle'
local sheet=Image(768,128,ColorMode.RGB)
for frame=1,4 do
 if frame>1 then s:newFrame() end
 local im=Image(192,128,ColorMode.RGB)
 local function rect(x,y,w,h,c)for yy=math.max(0,y),math.min(127,y+h-1)do for xx=math.max(0,x),math.min(191,x+w-1)do im:drawPixel(xx,yy,c)end end end
 local function line(x,y,u,v,c,w)local n=math.max(math.abs(u-x),math.abs(v-y));for i=0,n do rect(math.floor(x+(u-x)*i/math.max(1,n)),math.floor(y+(v-y)*i/math.max(1,n)),w or 1,w or 1,c)end end
 local function poly(p,c)
  for y=0,127 do local nodes={};local j=#p;for i=1,#p do local a,b=p[i],p[j];if(a[2]<=y and b[2]>y)or(b[2]<=y and a[2]>y)then nodes[#nodes+1]=a[1]+(y-a[2])/(b[2]-a[2])*(b[1]-a[1])end;j=i end;table.sort(nodes);for i=1,#nodes-1,2 do rect(math.ceil(nodes[i]),y,math.floor(nodes[i+1])-math.ceil(nodes[i])+1,1,c)end end
 end
 -- Braced white uniform, shoulder plates and a swept side-facing silhouette.
 poly({{26,128},{31,89},{44,72},{64,69},{88,78},{100,127}},ink)
 poly({{31,127},{35,91},{47,77},{62,74},{84,81},{94,127}},white)
 poly({{31,127},{39,97},{48,95},{46,116},{58,127}},grey)
 poly({{56,74},{58,58},{74,56},{74,74},{65,83}},ink)
 poly({{59,72},{61,58},{71,58},{71,73},{65,78}},shade)
 poly({{45,33},{54,22},{73,25},{82,36},{82,47},{88,53},{81,57},{79,66},{69,71},{56,62},{49,49}},ink)
 poly({{49,34},{56,26},{72,28},{79,37},{79,49},{85,53},{78,55},{76,64},{68,67},{58,59},{53,47}},skin)
 poly({{52,45},{58,48},{59,59},{68,66},{62,65},{54,58}},shade)
 -- Brown spikes echo the title hero, with a long forehead fringe.
 poly({{47,49},{37,34},{33,13},{47,20},{42,5},{60,16},{73,9},{86,2},{81,23},{84,30},{77,40},{73,30},{65,36},{61,29},{57,42},{53,37},{53,51}},ink)
 poly({{46,37},{38,20},{49,26},{47,13},{60,22},{73,16},{81,10},{77,25},{79,29},{76,34},{72,26},{64,31},{60,25},{55,36},{51,32},{50,44}},hair)
 line(43,23,51,31,gold);line(53,19,60,25,gold);line(73,20,67,28,gold)
 line(69,40,78,38,ink,2);poly({{69,43},{78,41},{77,47},{72,47}},white);rect(75,42,2,5,hair);rect(77,42,1,3,ink)
 line(75,59,80,58,ink);rect(51,47,4,6,skin);line(52,48,54,51,shade)
 -- Raised trigger arm and supporting arm extend to the right.
 poly({{49,80},{61,78},{82,87},{102,79},{111,88},{85,103},{62,98},{48,90}},ink)
 poly({{51,82},{61,81},{82,91},{101,83},{106,88},{84,99},{63,95},{51,88}},white)
 line(65,91,82,96,grey,3);line(51,82,49,88,grey,2)
 poly({{71,79},{83,79},{104,65},{115,70},{111,79},{88,91},{78,87}},ink)
 poly({{77,81},{84,82},{104,69},{111,72},{108,76},{87,87}},white)
 poly({{99,81},{105,76},{115,78},{117,85},{110,90},{105,87}},skin)
 line(109,80,114,82,shade);line(107,84,114,85,shade)
 -- Heavy sci-fi gun, angled grip, glowing charging rail and muzzle.
 poly({{94,60},{104,53},{142,53},{146,58},{162,58},{162,75},{128,76},{119,94},{109,91},{115,75},{99,74}},ink)
 poly({{99,61},{105,57},{141,57},{144,62},{157,62},{157,71},{124,72},{116,89},{113,88},{119,71},{101,70}},dark)
 rect(106,58,33,5,grey);rect(105,64,42,3,cyan);rect(149,58,5,16,ink);rect(153,61,7,11,grey);rect(158,62,3,9,cyan)
 rect(109,50,16,4,ink);rect(112,49,9,2,cyan);line(120,77,115,86,grey)
 poly({{53,75},{60,72},{65,81},{59,87}},dark);poly({{68,80},{74,73},{80,78},{73,88}},grey)
 line(58,101,61,124,dark,2);rect(67,110,16,8,dark);rect(69,112,12,3,cyan)
 -- Four increasingly bright charge frames, transparent outside the silhouette.
 local r=3+frame*2;local mx,my=169,66
 for a=0,359 do local t=a*math.pi/180;rect(math.floor(mx+math.cos(t)*r),math.floor(my+math.sin(t)*r),1,1,cyan)end
 for y=-r+2,r-2 do for x=-r+2,r-2 do if x*x+y*y<(r-2)*(r-2) then rect(mx+x,my+y,1,1,frame==4 and white or C(44,140+frame*20,224))end end end
 line(161,66,mx,66,white,2)
 if frame>=2 then line(mx-r-4,57,mx-r,61,cyan);line(mx+3,49,mx+1,55,white);line(mx+9,76,mx+13,81,cyan)end
 if frame==4 then line(177,66,191,66,white,2);line(169,51,169,46,cyan);line(169,80,169,85,cyan)end
 s.cels[frame].image=im;sheet:drawImage(im,Point((frame-1)*192,0));s.frames[frame].duration=.12
end
s:saveAs(root..'ArtSource/lucky_charge.aseprite')
sheet:saveAs(root..'Assets/GameAssets/Sprites/lucky_charge.png');s:close()
