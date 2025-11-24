namespace Project;

public class Scene
{
    public Model3d Model { get; set; } = new Model3d()
    {
        ObjectPath = @"/Assets/viking_room.obj",
        TexturePath = @"/Assets/viking_room.png",
        ShaderPath = @"/Assets/Shaders/shader.frag",
    };
}