local root="D:/GitHub/Unity/CounterToZero/"
local pc=app.pixelColor
local function rgb(r,g,b,a) return pc.rgba(r,g,b,a or 255) end
local function editHero(name,cell)
 local s=app.open(root.."Backup/2026-11-09-before-micro-circuit/man/"..name)
 local image=Image(s.cels[1].image)
 for y=0,image.height-1 do for x=0,image.width-1 do
  local p=image:getPixel(x,y);local r,g,b,a=pc.rgbaR(p),pc.rgbaG(p),pc.rgbaB(p),pc.rgbaA(p)
  local localY=y%cell-(cell==32 and 8 or 0)
  if a>0 then
   if r==59 and g==125 then p=rgb(246,247,239)
   elseif r==47 and g==87 then p=rgb(174,194,203)
   elseif r==82 and g==51 then p=rgb(67,43,35)
   elseif r==143 and g==77 then p=rgb(112,72,46)
   elseif localY>=7 and localY<=9 and ((r==64 and g==73) or (r==44 and g==53)) then p=rgb(110,61,29)
   end
   image:drawPixel(x,y,p)
  end
 end end
 for row=0,s.height/cell-1 do for col=0,2 do
  local ox=col*cell+(cell==32 and 8 or 0);local oy=row*cell+(cell==32 and 8 or 0)
  for _,v in ipairs({{5,0},{5,1},{4,1},{6,1},{9,0},{9,1},{10,1}})do image:drawPixel(ox+v[1],oy+v[2],rgb(67,43,35))end
  image:drawPixel(ox+5,1+oy,rgb(112,72,46))
 end end
 s.cels[1].image=image
 s:saveAs(root.."ArtSource/"..name:gsub("%.png$","")..".aseprite")
 s:saveCopyAs(root.."Assets/GameAssets/Sprites/man/"..name)
 s:close()
end
editHero("1 idle.png",16);editHero("1 walk.png",16);editHero("1 punch.png",32)
local palettes={{126,149,163},{56,158,166},{178,122,66}}
for variant=1,3 do
 local s=app.open(root.."Backup/2026-11-09-before-micro-circuit/man/8walk.png")
 local image=Image(s.cels[1].image);local base=palettes[variant]
 for y=0,image.height-1 do for x=0,image.width-1 do
  local p=image:getPixel(x,y);local r,g,b,a=pc.rgbaR(p),pc.rgbaG(p),pc.rgbaB(p),pc.rgbaA(p)
  if a>0 then
   if r==154 then p=rgb(base[1],base[2],base[3])
   elseif r==197 then p=rgb(220,234,238)
   elseif r==105 and g==102 then p=rgb(47,62,77)
   elseif r==70 then p=rgb(30,38,48)
   elseif (r==114 and g==75) or (r==180 and g==117) then p=rgb(255,35,52)
   end
   image:drawPixel(x,y,p)
  end
 end end
 for row=0,3 do for col=0,2 do
  local x,y=col*16,row*16
  -- Metallic faceplate, red optics, back cooling vents and illuminated core.
  if col==0 then
   for dx=4,11 do image:drawPixel(x+dx,y+7,rgb(22,26,35))end
   image:drawPixel(x+4,y+8,rgb(255,35,52));image:drawPixel(x+5,y+8,rgb(255,97,99));image:drawPixel(x+10,y+8,rgb(255,97,99));image:drawPixel(x+11,y+8,rgb(255,35,52))
   for dx=6,9 do image:drawPixel(x+dx,y+11,rgb(22,26,35))end
   image:drawPixel(x+7,y+13,rgb(255,35,52));image:drawPixel(x+8,y+13,rgb(255,35,52))
  elseif col==1 then
   for dy=5,9,2 do for dx=5,10 do image:drawPixel(x+dx,y+dy,rgb(30,38,48))end end
   image:drawPixel(x+7,y+12,rgb(255,35,52))
  else image:drawPixel(x+11,y+8,rgb(255,35,52));image:drawPixel(x+12,y+8,rgb(255,97,99)) end
  if variant==2 then image:drawPixel(x+7,y,rgb(255,35,52));image:drawPixel(x+7,y+1,rgb(220,234,238))end
  if variant==3 then image:drawPixel(x+1,y+12,rgb(178,122,66));image:drawPixel(x+14,y+12,rgb(178,122,66))end
 end end
 s.cels[1].image=image
 local name=variant==1 and "8walk" or variant==2 and "8walk_scout" or "8walk_heavy"
 s:saveAs(root.."ArtSource/"..name..".aseprite");s:saveCopyAs(root.."Assets/GameAssets/Sprites/man/"..name..".png");s:close()
end
print("Saved 3 hero sheets and 3 red-eye robot variants in Aseprite.")
