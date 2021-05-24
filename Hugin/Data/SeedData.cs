using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hugin.Services;

namespace Hugin.Data
{
    public class SeedData
    {
        public static async Task InitializeAsync(DatabaseContext context, UserHandleService userHandler, LectureHandleService lectureHander, SandboxTemplateHandleService sandboxTemplateHandle)
        {
            if(await context.Users.AnyAsync())
            {
                // DB has been seeded  
                return;
            }

            var admin = userHandler.AddNew(new User
            {
                Account = "admin",
                DisplayName = "管理者",
                EnglishName = "Administrator",
                Email = "admin@localhost",
                RawPassword = "password",
                IsAdmin = true,
                IsTeacher = true,
                IsLdapUser = false
            });
            var lecture = lectureHander.AddNew(new Lecture
            {
                OwnerId = admin.Id,
                Name = "demo",
                Subject = "Demo Lecture",
                Description = "for Demo",
                DefaultBranch = "master",
                IsActived = true,
                Opened = true
            });


            foreach (var i in Enumerable.Range(1, 3))
            {
                var user = userHandler.AddNew(new User
                {
                    Account = string.Format("test{0:000}", i),
                    DisplayName = string.Format("テストユーザー #{0:000}", i),
                    EnglishName = string.Format("Test User #{0:000}", i),
                    Email = string.Format("test{0:000}@localhost", i),
                    RawPassword = "password",
                    IsLdapUser = false
                });
                var rel = new LectureUserRelationship
                {
                    LectureId = lecture.Id,
                    UserId = user.Id,
                    Role = LectureUserRelationship.LectureRole.Student
                };
                context.LectureUserRelationships.Add(rel);
                context.SaveChanges();
            }

            sandboxTemplateHandle.AddNew(new SandboxTemplate
            {
                Name = "ruby-2.7.0",
                Description = "Ruby version 2.7.0",
                Commands = @"apt-get update && apt-get install -y build-essential libffi-dev zlib1g-dev libssl-dev libreadline-dev libgdbm-dev libbison-dev libmariadbclient-dev
mkdir /tmp
cd tmp
wget --no-check-certificate https://cache.ruby-lang.org/pub/ruby/2.7/ruby-2.7.0.tar.gz
tar xzf ruby-2.7.0.tar.gz
cd /tmp/ruby-2.7.0
./configure
make
make install"
            });
            sandboxTemplateHandle.AddNew(new SandboxTemplate
            {
                Name = "python-3.8.2",
                Description = "python version 3.8.2",
                Commands = @"apt-get update && apt-get install -y build-essential zlib1g-dev libncurses5-dev libgdbm-dev libnss3-dev libssl-dev libreadline-dev libffi-dev libbz2-dev lzma liblzma-dev cmake 
mkdir /tmp
cd /tmp
wget --no-check-certificate https://www.python.org/ftp/python/3.8.2/Python-3.8.2.tgz
tar xvfzp Python-3.8.2.tgz
cd /tmp/Python-3.8.2
./configure --enable-optimizations
make
make install
pip3 install --upgrade pip && pip3 install pandas numpy matplotlib"
            });
            sandboxTemplateHandle.AddNew(new SandboxTemplate
            {
                Name = "r-lang-3.3.3",
                Description = "R version 3.3.3; /usr/bin/Rscript --vanilla --quiet -e \"1 + 1\"",
                Commands = @"apt-get update && apt-get install -y build-essential libffi-dev zlib1g-dev libssl-dev libreadline-dev libgdbm-dev libbison-dev libmariadbclient-dev
mkdir /tmp
cd tmp
wget --no-check-certificate https://cran.r-project.org/src/base/R-4/R-4.0.2.tar.gz
tar xzvf R-4.0.2.tar.gz
cd /tmp/R-4.0.2
./configure
make
make install

cd /tmp; wget ftp://ftp.gnu.org/gnu/gcc/gcc-10.2.0/gcc-10.2.0.tar.gz; tar -xvzf gcc-10.2.0.tar.gz
cd /tmp/gcc-10.2.0
./contrib/download_prerequisites
./configure --enable-languages=c,c++,d,fortran --prefix=/usr/local --disable-bootstrap --disable-multilib; make; make install

apt-get install -y ca-certificates
apt-get install -y r-base

apt-get install -y locales
localedef -f UTF-8 -i ja_JP ja_JP.UTF-8"
            });
            sandboxTemplateHandle.AddNew(new SandboxTemplate
            {
                Name = "latex",
                Description = "LaTeX",
                Commands = @"apt-get update && apt-get install -y build-essential libffi-dev zlib1g-dev libssl-dev libreadline-dev libgdbm-dev libbison-dev libmariadbclient-dev
apt-get update && apt-get install -y texlive-lang-cjk xdvik-ja evince texlive-fonts-recommended texlive-fonts-extra"
            });
            sandboxTemplateHandle.AddNew(new SandboxTemplate
            {
                Name = "octave-5.2.0",
                Description = "Octave version 5.2.0",
                Commands = @"apt-get update
apt-get install -y build-essential libffi-dev zlib1g-dev libssl-dev libreadline-dev libgdbm-dev libbison-dev libmariadbclient-dev autoconf gfortran libblas-dev libatlas-dev liblapack-dev libpcre3-dev ghostscript fonts-ipafont
apt-get install -y gcc g++ gfortran make libblas-dev liblapack-dev libpcre3-dev libarpack2-dev libcurl4-gnutls-dev epstool libfftw3-dev transfig libfltk1.3-dev libfontconfig1-dev libfreetype6-dev libgl2ps-dev libglpk-dev libreadline-dev gnuplot libgraphicsmagick++1-dev libhdf5-serial-dev libsndfile1-dev llvm-dev lpr texinfo libglu1-mesa-dev pstoedit libjack0 libjack-dev portaudio19-dev libqhull-dev libqrupdate-dev libqscintilla2-dev libqt4-dev libqtcore4 libqtwebkit4 libqt4-network libqtgui4 libsuitesparse-dev zlib1g-dev libxft-dev autoconf automake bison flex gperf gzip librsvg2-bin icoutils libtool perl rsync tar libosmesa6-dev libqt4-opengl-dev

fc-cache -fv
mkdir /tmp
cd /tmp
wget --no-check-certificate wget http://ftp.jaist.ac.jp/pub/GNU/octave/octave-5.2.0.tar.xz
tar xvf octave-5.2.0.tar.xz
cd octave-5.2.0
autoconf
./configure --without-fontconfig --without-opengl --without-osmesa --without-qt --disable-docs
make
make install
# install control package
wget --no-check-certificate https://octave.sourceforge.io/download.php?package=control-3.2.0.tar.gz -O control-3.2.0.tar.gz
echo ""pkg install control - 3.2.0.tar.gz"" | octave -q -H"
            });
        }
    }
}
