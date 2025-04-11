import numpy as np
import pyvista as pv

# Load a surface to voxelize
folder1 = "C:/Data/Projects/Unity/LeniaUnity/Assets/External/Meshes"
filename1 = "beetle.obj"
surface = pv.read(folder1 + "/" + filename1)
surface

surface.plot(opacity=1.00)
voxels = pv.voxelize(surface, density=surface.length / 500)
voxels.plot(opacity=1.00)
