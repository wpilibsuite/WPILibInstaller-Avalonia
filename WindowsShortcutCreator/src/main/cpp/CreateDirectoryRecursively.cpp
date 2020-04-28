/* From http://blog.nuclex-games.com/2012/06/how-to-create-directories-recursively-with-win32/
 * Retrieved April 12, 2017
 * Posted by user Cygon (http://blog.nuclex-games.com/author/cygon/)
 *
 * This code is free for the taking and you can use it however you want.
 *
 * THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT
 * LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 *
 * As I just found out, the CreateDirectory function on Win32 can only create one directory at a time.
 * If one, for example, specifies C:\Users\All Users\FirstNew\SecondNew as the directory to create, and
 * both FirstNew and SecondNew do not exist, then CreateDirectory() fails.... Here's a workaround:
 */

#include <string>
#include <Windows.h>
#include <stdexcept>

/// <summary>Creates all directories down to the specified path</summary>
/// <param name="directory">Directory that will be created recursively</param>
/// <remarks>
///   The provided directory must not be terminated with a path separator.
/// </remarks>
bool createDirectoryRecursively(const std::wstring &directory) {
  static const std::wstring separators(L"\\/");

  // If the specified directory name doesn't exist, do our thing
  DWORD fileAttributes = ::GetFileAttributesW(directory.c_str());
  if(fileAttributes == INVALID_FILE_ATTRIBUTES) {

    // Recursively do it all again for the parent directory, if any
    std::size_t slashIndex = directory.find_last_of(separators);
    if(slashIndex != std::wstring::npos) {
      bool wasSuccessful = createDirectoryRecursively(directory.substr(0, slashIndex));
      if (!wasSuccessful) {
        return false;
      }
    }

    // Create the last directory on the path (the recursive calls will have taken
    // care of the parent directories by now)
    BOOL result = ::CreateDirectoryW(directory.c_str(), nullptr);
    return result;

  } else { // Specified directory name already exists as a file or directory

    bool isDirectoryOrJunction =
      ((fileAttributes & FILE_ATTRIBUTE_DIRECTORY) != 0) ||
      ((fileAttributes & FILE_ATTRIBUTE_REPARSE_POINT) != 0);

    return isDirectoryOrJunction;
  }
}
